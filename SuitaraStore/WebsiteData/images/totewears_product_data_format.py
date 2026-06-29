from selenium import webdriver
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.common.by import By
from webdriver_manager.chrome import ChromeDriverManager
from selenium.webdriver.support.ui import WebDriverWait
import csv
import json
import os
import re
import time
from urllib.parse import urljoin, urlparse
import requests

# ================= SETTINGS =================
BASE_URL = "https://totewears.com/collections/best-selling-%F0%9F%94%A5/"
CATEGORY_NAME = "bestselling-001"      # Filename prefix/category name
START_PRODUCT_CODE = 1        # First product code: 194, next 195, next 196...
OUTPUT_IMAGES_FOLDER = "images"
JSON_FILE = "totewears_products.json"
CSV_FILE = "totewears_products.csv"
HEADLESS = False
# ============================================

os.makedirs(OUTPUT_IMAGES_FOLDER, exist_ok=True)

options = webdriver.ChromeOptions()
if HEADLESS:
    options.add_argument("--headless=new")
options.add_argument("--start-maximized")
options.add_argument("--disable-blink-features=AutomationControlled")
options.add_argument("--log-level=3")

driver = webdriver.Chrome(service=Service(ChromeDriverManager().install()), options=options)
wait = WebDriverWait(driver, 15)


def clean_text(value):
    if not value:
        return ""
    return re.sub(r"\s+", " ", value).strip()


def full_url(url):
    if not url:
        return None
    url = url.strip()
    if url.startswith("//"):
        return "https:" + url
    return urljoin("https://totewears.com", url)


def best_image_from_srcset(srcset):
    if not srcset:
        return None
    items = []
    for part in srcset.split(","):
        chunk = part.strip().split(" ")[0].strip()
        if chunk:
            items.append(full_url(chunk))
    return items[-1] if items else None


def get_text(selector):
    try:
        return clean_text(driver.find_element(By.CSS_SELECTOR, selector).text)
    except Exception:
        return ""


def get_all_product_links():
    product_links = []
    page = 1

    while True:
        url = BASE_URL if page == 1 else f"{BASE_URL}?page={page}"
        print(f"Opening Listing Page: {url}")
        driver.get(url)
        time.sleep(3)

        links = []
        for a in driver.find_elements(By.CSS_SELECTOR, "a[href*='/products/']"):
            href = full_url(a.get_attribute("href"))
            if href and "/products/" in href and href not in links:
                # remove duplicate variants/query
                href = href.split("?")[0]
                if href not in links:
                    links.append(href)

        new_links = [x for x in links if x not in product_links]
        if not new_links:
            break

        product_links.extend(new_links)
        print(f"  Products Found: {len(new_links)} | Total: {len(product_links)}")

        # Agar next page ka product nahi mila to loop stop ho jayega
        page += 1
        if page > 100:
            break

    return product_links


def download_file(url, path):
    headers = {"User-Agent": "Mozilla/5.0"}
    r = requests.get(url, headers=headers, timeout=30)
    if r.status_code == 200 and r.content:
        with open(path, "wb") as f:
            f.write(r.content)
        return True
    return False


def extract_product_images(product_code):
    image_urls = []

    selectors = [
        "img.photoswipe__image",
        ".product__main-photos img",
        ".product__photos img",
    ]

    for sel in selectors:
        for img in driver.find_elements(By.CSS_SELECTOR, sel):
            src = (
                img.get_attribute("data-photoswipe-src")
                or best_image_from_srcset(img.get_attribute("data-srcset"))
                or best_image_from_srcset(img.get_attribute("srcset"))
                or img.get_attribute("src")
            )
            src = full_url(src)
            if src and "cdn/shop/files" in src and src not in image_urls:
                image_urls.append(src)
        if image_urls:
            break

    downloaded_images = []

    for img_index, img_url in enumerate(image_urls, start=1):
        # Required format: Leat-001-194-00001.jpg
        image_name = f"{CATEGORY_NAME}-{product_code}-{str(img_index).zfill(5)}.jpg"
        image_path = os.path.join(OUTPUT_IMAGES_FOLDER, image_name)

        try:
            if download_file(img_url, image_path):
                downloaded_images.append(image_name)
                print(f"    Downloaded: {image_name}")
        except Exception as e:
            print(f"    Failed: {img_url} | {e}")

    return image_urls, downloaded_images


def scrape_product(product_url, product_code):
    driver.get(product_url)
    time.sleep(3)

    name = get_text("h1.product-single__title") or get_text("h1")
    original_price = get_text("span[id^='ComparePrice']") or get_text(".product__price--compare")
    sale_price = get_text("span[id^='ProductPrice']") or get_text(".product__price.on-sale")
    discount = get_text("span[id^='SavePrice']") or get_text(".product__price-savings")
    description = get_text(".product-single__description")

    variant_id = ""
    try:
        variant_id = driver.find_element(By.CSS_SELECTOR, "select[name='id'] option").get_attribute("value") or ""
    except Exception:
        try:
            variant_id = driver.find_element(By.CSS_SELECTOR, "input[name='id']").get_attribute("value") or ""
        except Exception:
            pass

    image_urls, images = extract_product_images(product_code)

    return {
        "CategoryName": CATEGORY_NAME,
        "ProductCode": product_code,
        "ProductName": name,
        "OriginalPrice": original_price,
        "SalePrice": sale_price,
        "Discount": discount,
        "Description": description,
        "VariantId": variant_id,
        "ProductUrl": product_url,
        "ProductImages": images,
        "ImageUrls": image_urls,
    }


try:
    links = get_all_product_links()
    print(f"\nTotal Product Links: {len(links)}")

    products = []
    for i, link in enumerate(links):
        product_code = START_PRODUCT_CODE + i
        print(f"\nProduct Code {product_code}: {link}")
        try:
            products.append(scrape_product(link, product_code))
        except Exception as e:
            print(f"  Product Failed: {e}")

    with open(JSON_FILE, "w", encoding="utf-8") as f:
        json.dump({"totalProducts": len(products), "products": products}, f, indent=2, ensure_ascii=False)

    with open(CSV_FILE, "w", newline="", encoding="utf-8-sig") as f:
        writer = csv.DictWriter(f, fieldnames=[
            "CategoryName", "ProductCode", "ProductName", "OriginalPrice", "SalePrice",
            "Discount", "Description", "VariantId", "ProductUrl", "ProductImages"
        ])
        writer.writeheader()
        for p in products:
            row = p.copy()
            row["ProductImages"] = " | ".join(p.get("ProductImages", []))
            row.pop("ImageUrls", None)
            writer.writerow(row)

    print("\nDONE")
    print(f"Images Folder: {OUTPUT_IMAGES_FOLDER}")
    print(f"JSON File: {JSON_FILE}")
    print(f"CSV File: {CSV_FILE}")

finally:
    driver.quit()
