using DynamicEcomDZ.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Net.Http;

namespace DynamicEcomDZ.Controllers
{
    public class StoreDetailController : Controller
    {
        private readonly IConfiguration _config;

        public StoreDetailController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Abouts()
        {
            return View();
        }

        // =====================================================================
        //  INDEX  —  Single Store (default id = 1)
        // =====================================================================
        public async Task<IActionResult> Index(int id = 1)
        {
            string conStr = _config.GetConnectionString("Connection1");

            var sliders = new List<DetailSliderModel>();
            var products = new List<DetailModel>();
            string subHeaderTitle = "";

            var viewModel = new RestaurantDetailViewModel
            {
                RestaurantId = id
            };

            using (SqlConnection con = new SqlConnection(conStr))
            {
                await con.OpenAsync();

                // ---------- SLIDER DATA ----------
                using (SqlCommand cmd = new SqlCommand("Redirection_TAB_SP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nType", 0);
                    cmd.Parameters.AddWithValue("@nsType", 4);
                    cmd.Parameters.AddWithValue("@CustomerId", "1");
                    cmd.Parameters.AddWithValue("@SliderType", "Customer");

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            sliders.Add(new DetailSliderModel
                            {
                                SliderId = Convert.ToInt32(dr["SliderId"]),
                                SliderName = dr["SliderName"]?.ToString(),
                                SliderMovingTimer = dr["SliderMovingTimer"] == DBNull.Value
                                                        ? 5
                                                        : Convert.ToInt32(dr["SliderMovingTimer"]),
                                HeadingSlider = dr["HeadingSlider"]?.ToString(),
                                DescriptionSlider = dr["DescriptionSlider"]?.ToString()
                            });

                            subHeaderTitle = dr["SubHeader"]?.ToString() ?? "";
                        }
                    }
                }

                // ---------- STORE DETAIL (nsType = 6) ----------
                using (SqlCommand cmd = new SqlCommand("Redirection_TAB_SP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nType", 0);
                    cmd.Parameters.AddWithValue("@nsType", 6);
                    cmd.Parameters.AddWithValue("@CustomerId", "1");

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (await dr.ReadAsync())
                        {
                            viewModel.RestaurantName = dr["Customer"]?.ToString() ?? "Store";
                            viewModel.RestaurantAddress = dr["Address"]?.ToString() ?? "";
                            viewModel.RestaurantLogo = dr["Logo"]?.ToString() ?? "";
                        }
                    }
                }

                // ---------- USER / STORE INFO — User_SP (nsCategoryId = 3) ----------
                using (SqlCommand cmd = new SqlCommand("User_SP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nCategoryId", 0);
                    cmd.Parameters.AddWithValue("@nsCategoryId", 3);

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (await dr.ReadAsync())
                        {
                            viewModel.CompanyName = dr["CompanyName"]?.ToString() ?? "";
                            viewModel.Address = dr["Address"]?.ToString() ?? "";
                            viewModel.HeaderLogo = dr["Headerlogo"]?.ToString() ?? "";
                            viewModel.FooterLogo = dr["Footerlogo"]?.ToString() ?? "";
                            viewModel.ClosingTimeIn = dr["ClosingTimeIn"]?.ToString() ?? "";
                            viewModel.ClosingTimeOut = dr["ClosingTimeOut"]?.ToString() ?? "";
                            viewModel.DeliveryCharges = dr["DeliveryCharges"] == DBNull.Value
                                                        ? 0
                                                        : Convert.ToDecimal(dr["DeliveryCharges"]);
                            viewModel.PhoneNo = dr["PhoneNo"]?.ToString() ?? "";
                            viewModel.WhatsappNo = dr["WhatsappNo"]?.ToString() ?? "";
                        }
                    }
                }

                // ---------- PRODUCT DATA (nsType = 3) ----------
                using (SqlCommand cmd = new SqlCommand("Redirection_TAB_SP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nType", 0);
                    cmd.Parameters.AddWithValue("@nsType", 3);
                    cmd.Parameters.AddWithValue("@CustomerId", "1");

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            products.Add(new DetailModel
                            {
                                Category = dr["Category"]?.ToString(),
                                ProductName = dr["Product"]?.ToString(),
                                ProductCode = dr["ProductCode"]?.ToString(),
                                ProductDescription = dr["ProductDescription"]?.ToString(),
                                ProductImage = dr["ProductImage"]?.ToString(),
                                Prices = dr["Prices"] == DBNull.Value
                                                     ? 0 : Convert.ToDecimal(dr["Prices"]),
                                DiscountAmount = dr["DiscountAmount"] == DBNull.Value
                                                     ? 0 : Convert.ToDecimal(dr["DiscountAmount"]),
                                CustomerName = dr["CustomerName"]?.ToString(),
                                DeliveryCharges = dr["DeliveryCharges"] == DBNull.Value
                                                     ? 0 : Convert.ToDecimal(dr["DeliveryCharges"])
                            });
                        }
                    }
                }
            }

            // ---------- VIEWMODEL COMPLETE ----------
            viewModel.Sliders = sliders;
            viewModel.SubHeaderTitle = subHeaderTitle;
            viewModel.Products = products
                                         .GroupBy(p => p.Category)
                                         .ToDictionary(g => g.Key, g => g.ToList());

            // Fallback: agar nsType=6 se name nahi mila
            if (string.IsNullOrEmpty(viewModel.RestaurantName))
                viewModel.RestaurantName = products.FirstOrDefault()?.CustomerName ?? "Store";

            // Fallback: agar User_SP se CompanyName nahi mila
            if (string.IsNullOrEmpty(viewModel.CompanyName))
                viewModel.CompanyName = viewModel.RestaurantName;

            return View(viewModel);
        }

        // =====================================================================
        //  PLACE ORDER
        // =====================================================================
        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderRequest model)
        {
            if (model == null || model.Items == null || !model.Items.Any())
                return BadRequest(new MessageResponse { StatusId = 0, Message = "Invalid order data." });

            await Task.Delay(2000); // Remove in production

            string conStr = _config.GetConnectionString("Connection1");
            string MasterXML = "";
            string DetailXML = "";

            // Build Master XML
            string[] masterParams = { "ContactNo", "CustomerName", "CustomerAddress", "Street", "Floor", "Description" };
            string[] masterValues =
            {
                model.ContactNo       ?? "",
                model.CustomerName    ?? "",
                model.CustomerAddress ?? "",
                model.Street          ?? "",
                model.Floor           ?? "",
                model.Description     ?? ""
            };
            MasterXML = $"<Insert1>{CreateXML(masterParams, masterValues, "MasterXML")}</Insert1>";

            // Build Detail XML
            foreach (var item in model.Items)
            {
                string[] detailParams = { "ProductCode", "Quantity", "Rate", "DiscountAmount" };
                string[] detailValues =
                {
                    item.ProductCode              ?? "",
                    item.Quantity.ToString()      ?? "0",
                    item.Rate.ToString()          ?? "0",
                    item.DiscountAmount.ToString() ?? "0"
                };
                DetailXML += CreateXML(detailParams, detailValues, "DetailXML");
            }
            DetailXML = $"<Insert1>{DetailXML}</Insert1>";

            var response = new MessageResponse();
            string whatsappApiUrl = "";   // ← DB se return hone wala WhatsApp API URL

            using (SqlConnection con = new SqlConnection(conStr))
            {
                string sql = $@"
                    EXEC EcomOrder_SP
                        @nType           = 0,
                        @nsType          = 0,
                        @MasterXML       = '{MasterXML.Replace("'", "''")}',
                        @DetailXML       = '{DetailXML.Replace("'", "''")}',
                        @DeliveryCharges = {model.Items[0].DeliveryCharges},
                        @RestaurantId    = {model.RestaurantId}
                ";

                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(sql, con))
                {
                    cmd.CommandType = CommandType.Text;

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (dr.Read())
                        {
                            response.StatusId = dr["StatusId"] != DBNull.Value
                                                ? Convert.ToInt32(dr["StatusId"]) : 0;
                            response.Message = dr["Message"] != DBNull.Value
                                                ? dr["Message"].ToString() : "";

                            // ← WhatsApp API URL read karo
                            whatsappApiUrl = dr["WhatsappApi"] != DBNull.Value
                                                ? dr["WhatsappApi"].ToString() : "";
                        }
                        else
                        {
                            response.StatusId = 0;
                            response.Message = "Order could not be placed. No response from server.";
                        }
                    }
                }
            }

            // ← Order success ho aur URL mile to WhatsApp API GET hit karo
            if (response.StatusId == 1 && !string.IsNullOrWhiteSpace(whatsappApiUrl))
            {
                try
                {
                    using (HttpClient http = new HttpClient())
                    {
                        http.Timeout = TimeSpan.FromSeconds(10);
                        await http.GetAsync(whatsappApiUrl);
                    }
                }
                catch (Exception ex)
                {
                    // WhatsApp fail hone se order response affect na ho
                    Console.WriteLine("WhatsApp API Error: " + ex.Message);
                }
            }

            return Ok(response);
        }

        // =====================================================================
        //  CUSTOMER EXIST OR NOT
        // =====================================================================
        [HttpPost]
        public async Task<IActionResult> CustomerExsistOrNo([FromBody] OrderRequest model)
        {
            if (model == null)
                return BadRequest(new { StatusId = 0, Message = "Invalid request." });

            string conStr = _config.GetConnectionString("Connection1");

            using (SqlConnection con = new SqlConnection(conStr))
            {
                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("Customer_SP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@nType", SqlDbType.Int).Value = 0;
                    cmd.Parameters.Add("@nsType", SqlDbType.Int).Value = 4;
                    cmd.Parameters.Add("@ContactNo", SqlDbType.VarChar).Value = model.ContactNo;

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (dr.Read())
                        {
                            return Ok(new
                            {
                                StatusId = Convert.ToInt32(dr["StatusId"]),
                                Message = dr["Message"].ToString(),
                                CustomerName = dr["CustomerName"].ToString(),
                                CustomerAddress = dr["CustomerAddress"].ToString(),
                                Street = dr["Street"].ToString(),
                                Floor = dr["Floor"].ToString(),
                                Description = dr["Description"].ToString()
                            });
                        }
                    }
                }
            }

            return Ok(new { StatusId = 0, Message = "Customer not found." });
        }

        // =====================================================================
        //  CHECK STORE VALIDATION
        // =====================================================================
        [HttpPost]
        public async Task<IActionResult> CheckRestaurantValidation([FromBody] CheckRequest model)
        {
            string conStr = _config.GetConnectionString("Connection1");

            using (SqlConnection con = new SqlConnection(conStr))
            {
                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("Redirection_TAB_SP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nType", 0);
                    cmd.Parameters.AddWithValue("@nsType", 5);
                    cmd.Parameters.AddWithValue("@CustomerId", "1");

                    using (SqlDataReader dr = await cmd.ExecuteReaderAsync())
                    {
                        if (dr.Read())
                        {
                            return Ok(new
                            {
                                statusId = Convert.ToInt32(dr["StatusId"]),
                                message = dr["Message"].ToString()
                            });
                        }
                    }
                }
            }

            return Ok(new { statusId = 0, message = "Store is currently unavailable." });
        }

        // =====================================================================
        //  HELPER — XML BUILDER
        // =====================================================================
        private string CreateXML(string[] parameters, string[] values, string xmlTag)
        {
            try
            {
                string attrs = "";
                for (int i = 0; i < parameters.Length; i++)
                    attrs += $"{parameters[i]} = '{values[i]}' ";

                attrs = attrs.Replace("'", "\"");
                return $"<{xmlTag} {attrs}/>";
            }
            catch
            {
                return "";
            }
        }
    }

    // =====================================================================
    //  REQUEST MODELS
    // =====================================================================
    public class CheckRequest
    {
        public int RestaurantId { get; set; }
    }
}
