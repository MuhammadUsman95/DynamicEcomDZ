from selenium import webdriver
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.common.by import By
from webdriver_manager.chrome import ChromeDriverManager
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from urllib.parse import urljoin
import json
import time
import os
import re
import requests

# ============ SETUP ============
options = webdriver.ChromeOptions()
options.add_argument("--start-maximized")
# options.add_argument("--headless=new")  # Background me run karna ho to uncomment kar dena

driver = webdriver.Chrome(service=Service(ChromeDriverManager().install()), options=options)

base_url = "https://totewears.com/collections/cross-body-bags/"
all_products = []

os.makedirs("thumbnails", exist_ok=True)
os.makedirs("images", exist_ok=True)

# ============ HELPERS ============
def full_url(url):
    if not url:
        return None
    url = url.strip().strip('"').strip("'")
    if url.startswith("//"):
        return "https:" + url
    return urljoin(base_url, url)


def get_largest_from_srcset(srcset):
    if not srcset:
        return None

    best_url = None
    best_width = 0

    # Example: //totewears.com/cdn/shop/files/6_180x.webp?v=... 180w 270h,
    for part in srcset.split(','):
        part = part.strip()
        if not part:
            continue
        m_url = re.search(r'(https?:)?//[^\s,]+', part)
        if not m_url:
            continue

        url = full_url(m_url.group(0))
        m_w = re.search(r'(\d+)w', part)
        width = int(m_w.group(1)) if m_w else 0

        if width >= best_width:
            best_width = width
            best_url = url

    return best_url


def extract_image_from_element(el):
    # 1) background-image: url("...")
    style = el.get_attribute("style") or ""
    m = re.search(r'url\(["\']?(.*?)["\']?\)', style)
    if m:
        return full_url(m.group(1))

    # 2) data-bgset / data-srcset / srcset
    for attr in ["data-bgset", "data-srcset", "srcset"]:
        val = el.get_attribute(attr)
        img = get_largest_from_srcset(val)
        if img:
            return img

    # 3) img tags inside
    imgs = el.find_elements(By.TAG_NAME, "img")
    for img in imgs:
        for attr in ["src", "data-src", "data-original", "data-srcset", "srcset"]:
            val = img.get_attribute(attr)
            if not val:
                continue
            if "srcset" in attr:
                found = get_largest_from_srcset(val)
                if found:
                    return found
            else:
                return full_url(val)

    return None


def download_image(url, folder, filename):
    if not url:
        return False
    try:
        headers = {"User-Agent": "Mozilla/5.0"}
        r = requests.get(full_url(url), headers=headers, timeout=30)

        content_type = (r.headers.get("Content-Type") or "").lower()
        if r.status_code == 200 and r.content and content_type.startswith("image/"):
            with open(os.path.join(folder, filename), "wb") as f:
                f.write(r.content)
            return True
    except Exception as ex:
        print(f"      Download failed: {ex}")
    return False


def clean_image_url(url):
    """CDN URL ko clean karke duplicate images avoid karta hai."""
    url = full_url(url)
    if not url:
        return None

    # Query string remove, same image duplicate na ho
    url = url.split("?")[0]

    # Shopify size suffix remove: _180x, _360x, _720x, _master etc.
    url = re.sub(r'_(\d+x\d*|x\d+|\d+x|master|small|medium|large|grande|compact)(?=\.)', '', url, flags=re.I)

    return url


def get_shopify_product_images(product_url):
    """
    Shopify product .js endpoint se sirf product gallery images uthata hai.
    Ye method logo/banner/wear extra images ko avoid karta hai.
    """
    urls = []
    try:
        js_url = product_url.split("?")[0].rstrip("/") + ".js"
        headers = {"User-Agent": "Mozilla/5.0", "Accept": "application/json"}
        r = requests.get(js_url, headers=headers, timeout=25)

        if r.status_code == 200 and r.text.strip().startswith("{"):
            data = r.json()
            images = data.get("images") or []

            for img in images:
                if isinstance(img, dict):
                    u = img.get("src")
                else:
                    u = img

                u = clean_image_url(u)
                if u and "/cdn/shop/" in u and u not in urls:
                    urls.append(u)

    except Exception as ex:
        print(f"  Shopify .js images not found: {ex}")

    return urls


def get_product_gallery_images_from_page(driver):
    """Fallback: page ke product/gallery container se multiple images uthata hai."""
    urls = []

    # Page scroll se lazy loaded gallery images load ho jati hain
    try:
        driver.execute_script("window.scrollTo(0, document.body.scrollHeight / 2);")
        time.sleep(1)
        driver.execute_script("window.scrollTo(0, 0);")
        time.sleep(1)
    except Exception:
        pass

    image_selectors = [
        ".product__photos img",
        ".product-single__photos img",
        ".product__main-photos img",
        ".product__thumb img",
        ".product__media img",
        ".product-gallery img",
        ".product-single__media img",
        "[data-product-single-media-wrapper] img",
        "[data-media-id] img",
        ".photoswipe__image"
    ]

    skip_alt_words = [
        "logo", "brand", "icon", "banner", "footer", "header", "sprite"
    ]

    for sel in image_selectors:
        try:
            for img in driver.find_elements(By.CSS_SELECTOR, sel):
                alt = (img.get_attribute("alt") or "").lower()
                if any(w in alt for w in skip_alt_words):
                    continue

                found_url = None
                for attr in ["data-srcset", "srcset", "data-photoswipe-src", "data-zoom", "data-src", "src"]:
                    val = img.get_attribute(attr)
                    if not val:
                        continue

                    found_url = get_largest_from_srcset(val) if "srcset" in attr else full_url(val)
                    if found_url:
                        break

                found_url = clean_image_url(found_url)
                if found_url and "/cdn/shop/" in found_url and found_url not in urls:
                    urls.append(found_url)
        except Exception:
            pass

    return urls


def get_text_safe(parent, selectors):
    for sel in selectors:
        try:
            txt = parent.find_element(By.CSS_SELECTOR, sel).text.strip()
            if txt:
                return txt
        except Exception:
            pass
    return None


def get_attr_safe(parent, selectors, attr):
    for sel in selectors:
        try:
            val = parent.find_element(By.CSS_SELECTOR, sel).get_attribute(attr)
            if val:
                return full_url(val)
        except Exception:
            pass
    return None

# ============ STEP 1: TOTAL PAGES ============
driver.get(base_url)
WebDriverWait(driver, 20).until(
    EC.presence_of_all_elements_located((By.CSS_SELECTOR, ".grid-product, .grid__item"))
)
time.sleep(2)

try:
    page_numbers = []
    for a in driver.find_elements(By.CSS_SELECTOR, "a[href*='page='], .pagination a, .pagination__page"):
        txt = (a.text or "").strip()
        if txt.isdigit():
            page_numbers.append(int(txt))
    total_pages = max(page_numbers) if page_numbers else 1
except Exception:
    total_pages = 1

print(f"Total Pages: {total_pages}")

# ============ STEP 2: LISTING PAGES SE PRODUCTS ============
for page in range(1, total_pages + 1):
    url = base_url if page == 1 else f"{base_url}?page={page}"
    print(f"\n--- Page {page}/{total_pages} ---")
    driver.get(url)

    try:
        WebDriverWait(driver, 20).until(
            EC.presence_of_all_elements_located((By.CSS_SELECTOR, ".grid-product, .grid__item"))
        )
    except Exception:
        continue

    time.sleep(2)

    cards = driver.find_elements(By.CSS_SELECTOR, ".grid-product")
    if not cards:
        cards = [x for x in driver.find_elements(By.CSS_SELECTOR, ".grid__item") if x.find_elements(By.CSS_SELECTOR, ".grid-product__meta")]

    added = 0
    for product in cards:
        name = get_text_safe(product, [".grid-product__title", ".grid-product__title--heading"])
        if not name:
            continue

        original_price = get_text_safe(product, [".grid-product__price--original"])

        # Price div me original + sale dono hotay hain; sale price direct text clean kiya hai
        sale_price = None
        try:
            price_div = product.find_element(By.CSS_SELECTOR, ".grid-product__price")
            sale_price = price_div.text.strip()
            if original_price:
                sale_price = sale_price.replace(original_price, "").strip()
            sale_price = re.sub(r"Save\s*\d+%", "", sale_price, flags=re.I).strip()
            sale_price = sale_price.replace("Regular price", "").replace("Sale price", "").strip()
        except Exception:
            pass

        discount = get_text_safe(product, [".grid-product__price--savings"])
        after_discount = None

        product_url = get_attr_safe(product, ["a[href*='/products/']", "a.grid-product__link"], "href")

        thumb = None
        for sel in [".grid-product__secondary-image", ".grid-product__image-mask", ".image-wrap", "img"]:
            try:
                els = product.find_elements(By.CSS_SELECTOR, sel)
                for el in els:
                    thumb = extract_image_from_element(el)
                    if thumb:
                        break
                if thumb:
                    break
            except Exception:
                pass

        all_products.append({
            "name": name,
            "originalPrice": original_price,
            "salePrice": sale_price,
            "discount": discount,
            "afterDiscount": after_discount,
            "productUrl": product_url,
            "thumbnail": thumb
        })
        added += 1

    print(f"  Products: {added}")

print(f"\nTotal products: {len(all_products)}")

# ============ STEP 3: PRODUCT DETAIL + DOWNLOAD IMAGES ============
print("\n========== Fetching Details + Downloading ==========\n")

seen_products = set()
unique_products = []
for p in all_products:
    key = p.get("productUrl") or p.get("name")
    if key not in seen_products:
        seen_products.add(key)
        unique_products.append(p)
all_products = unique_products

for i, prod in enumerate(all_products):
    product_num = str(i + 49).zfill(3)

    if not prod.get("productUrl"):
        prod["description"] = None
        prod["specifications"] = []
        prod["thumbnailImage"] = None
        prod["productImages"] = []
        continue

    print(f"[Product {product_num}/{len(all_products)}] {prod['name'][:50]}...")

    thumb_name = f"Cross-001-{product_num}-00001.webp"
    if prod.get("thumbnail") and download_image(prod["thumbnail"], "thumbnails", thumb_name):
        prod["thumbnailImage"] = thumb_name
        print(f"  Thumb: {thumb_name}")
    else:
        prod["thumbnailImage"] = None

    try:
        driver.get(prod["productUrl"])
        time.sleep(3)

        # Description
        desc = get_text_safe(driver, [
            ".product-single__description",
            ".product__description",
            "#ProductDescription",
            ".rte",
            ".collapsible-content__inner",
            ".accordion-content"
        ])
        prod["description"] = desc

        specs = []
        for sel in [".product-single__description li", ".product__description li", ".rte li", ".accordion-content li"]:
            try:
                for li in driver.find_elements(By.CSS_SELECTOR, sel):
                    t = li.text.strip()
                    if t and t not in specs:
                        specs.append(t)
            except Exception:
                pass
        prod["specifications"] = specs

        # Detail images - ONLY PRODUCT IMAGES
        # Pehle Shopify product .js endpoint se images uthao.
        # Is endpoint me normally sirf product gallery images hoti hain,
        # is liye logo / wear / banner extra images download nahi hoti.
        img_urls = get_shopify_product_images(prod["productUrl"])

        # Agar .js endpoint se images na milen to page gallery se fallback lo
        if not img_urls:
            img_urls = get_product_gallery_images_from_page(driver)

        print(f"  Found product images: {len(img_urls)}")

        image_names = []
        for idx, img_url in enumerate(img_urls, start=1):
            img_num = str(idx).zfill(5)
            img_name = f"Tote-001-{product_num}-{img_num}.webp"
            if download_image(img_url, "images", img_name):
                image_names.append(img_name)
                print(f"    Image: {img_name}")

        prod["productImages"] = image_names
        print(f"  -> {len(image_names)} images, Desc: {'Yes' if prod['description'] else 'No'}")

    except Exception as e:
        prod["description"] = None
        prod["specifications"] = []
        prod["productImages"] = []
        print(f"  -> Error: {e}")

driver.quit()

# ============ STEP 4: JSON ============
json_products = []
for prod in all_products:
    json_products.append({
        "name": prod.get("name"),
        "originalPrice": prod.get("originalPrice"),
        "salePrice": prod.get("salePrice"),
        "discount": prod.get("discount"),
        "afterDiscount": prod.get("afterDiscount"),
        "productUrl": prod.get("productUrl"),
        "description": prod.get("description"),
        "specifications": prod.get("specifications", []),
        "thumbnailImage": prod.get("thumbnailImage"),
        "productImages": prod.get("productImages", [])
    })

final_json = {
    "totalProducts": len(json_products),
    "totalPages": total_pages,
    "scrapedAt": time.strftime("%Y-%m-%d %H:%M:%S"),
    "thumbnailFolder": "thumbnails/",
    "imagesFolder": "images/",
    "products": json_products
}

with open("totewears_COMPLETE.json", "w", encoding="utf-8") as f:
    json.dump(final_json, f, indent=2, ensure_ascii=False)

print(f"\n{'='*60}")
print("  SCRAPING COMPLETE!")
print(f"  Total Products: {len(json_products)}")
print("  Thumbnails: thumbnails/")
print("  Images: images/")
print("  JSON: totewears_COMPLETE.json")
print(f"{'='*60}")
