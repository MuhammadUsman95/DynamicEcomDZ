using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace CampaignManagement.Controllers
{
    public class WhatsappController : Controller
    {
        private readonly IConfiguration _config;

        public WhatsappController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Template()
        {
            return View();
        }

        public IActionResult Number()
        {
            return View();
        }

        public IActionResult Log()
        {
            return View();
        }

        // =========================
        // TEMPLATE EXCEL DOWNLOAD
        // =========================
        public IActionResult DownloadTemplateFormat()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("WhatsappTemplates");

            ws.Cell(1, 1).Value = "Template";
            ws.Cell(1, 2).Value = "IsActive";

            ws.Cell(2, 1).Value = "Hello {{Name}}, your campaign is ready.";
            ws.Cell(2, 2).Value = 1;

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "WhatsappTemplateFormat.xlsx"
            );
        }

        // =========================
        // TEMPLATE EXCEL IMPORT
        // =========================
        [HttpPost]
        public IActionResult ImportTemplate(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    TempData["Error"] = "Please select Excel file";
                    return RedirectToAction("Template");
                }

                string? conStr = _config.GetConnectionString("DefaultConnection");

                if (string.IsNullOrWhiteSpace(conStr))
                {
                    TempData["Error"] = "Connection string not found in appsettings.json";
                    return RedirectToAction("Template");
                }

                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);

                var ws = workbook.Worksheets.FirstOrDefault();

                if (ws == null)
                {
                    TempData["Error"] = "Excel worksheet not found";
                    return RedirectToAction("Template");
                }

                using SqlConnection con = new SqlConnection(conStr);
                con.Open();

                int row = 2;
                int importedCount = 0;

                while (!ws.Cell(row, 1).IsEmpty())
                {
                    string template = ws.Cell(row, 1).GetString()?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(template))
                    {
                        row++;
                        continue;
                    }

                    bool isActive = true;
                    string activeValue = ws.Cell(row, 2).GetString()?.Trim().ToLower() ?? "1";

                    if (activeValue == "0" || activeValue == "false" || activeValue == "no")
                        isActive = false;

                    using SqlCommand cmd = con.CreateCommand();

                    cmd.CommandText = "WhatsappTemplateSP";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@nType", 0);
                    cmd.Parameters.AddWithValue("@nsType", 0);
                    cmd.Parameters.AddWithValue("@Template", template);
                    cmd.Parameters.AddWithValue("@IsActive", isActive);

                    cmd.ExecuteNonQuery();

                    importedCount++;
                    row++;
                }

                TempData["Success"] = importedCount + " templates imported successfully";
                return RedirectToAction("Template");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.ToString();
                return RedirectToAction("Template");
            }
        }

        // =========================
        // NUMBER EXCEL DOWNLOAD
        // =========================
        public IActionResult DownloadNumberFormat()
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("WhatsappNumbers");

            ws.Cell(1, 1).Value = "DeliveryName";
            ws.Cell(1, 2).Value = "DeliveryPhone";

            ws.Cell(2, 1).Value = "Farman Shah";
            ws.Cell(2, 2).Value = "3170185920";

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "WhatsappNumberFormat.xlsx"
            );
        }

        // =========================
        // NUMBER EXCEL IMPORT
        // =========================
        [HttpPost]
        public IActionResult ImportNumber(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    TempData["Error"] = "Please select Excel file";
                    return RedirectToAction("Number");
                }

                string? conStr = _config.GetConnectionString("DefaultConnection");

                if (string.IsNullOrWhiteSpace(conStr))
                {
                    TempData["Error"] = "Connection string not found in appsettings.json";
                    return RedirectToAction("Number");
                }

                using var stream = file.OpenReadStream();
                using var workbook = new XLWorkbook(stream);

                var ws = workbook.Worksheets.FirstOrDefault();

                if (ws == null)
                {
                    TempData["Error"] = "Excel worksheet not found";
                    return RedirectToAction("Number");
                }

                using SqlConnection con = new SqlConnection(conStr);
                con.Open();

                int row = 2;
                int importedCount = 0;

                while (!ws.Cell(row, 1).IsEmpty() || !ws.Cell(row, 2).IsEmpty())
                {
                    string deliveryName = ws.Cell(row, 1).GetString()?.Trim() ?? "";
                    string deliveryPhone = ws.Cell(row, 2).GetString()?.Trim() ?? "";

                    deliveryPhone = deliveryPhone.Replace("'", "").Trim();

                    if (string.IsNullOrWhiteSpace(deliveryPhone))
                    {
                        row++;
                        continue;
                    }

                    using SqlCommand cmd = con.CreateCommand();

                    cmd.CommandText = "WhatsappNumberSP";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@nType", 0);
                    cmd.Parameters.AddWithValue("@nsType", 0);
                    cmd.Parameters.AddWithValue("@DeliveryName", deliveryName);
                    cmd.Parameters.AddWithValue("@DeliveryPhone", deliveryPhone);
                    cmd.Parameters.AddWithValue("@IsActive", true);

                    cmd.ExecuteNonQuery();

                    importedCount++;
                    row++;
                }

                TempData["Success"] = importedCount + " numbers imported successfully";
                return RedirectToAction("Number");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.ToString();
                return RedirectToAction("Number");
            }
        }
    }
}