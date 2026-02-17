using DynamicEcomDZ.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DynamicEcomDZ.Models;
using static DynamicEcomDZ.Controllers.RestaurantDetailController;

namespace DynamicEcomDZ.Controllers
{
    public class RestaurantDetailController : Controller
    {
        private readonly IConfiguration _config;
        public int Restaurantid;
        public string? subHeaderTitle = "";

        public RestaurantDetailController(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> Index(int id)
        {
            string conStr = _config.GetConnectionString("Connection1");

            var sliders = new List<DetailSliderModel>();
            var products = new List<DetailModel>();

            using (SqlConnection con = new SqlConnection(conStr))
            {
                await con.OpenAsync();

                // ================= SLIDER DATA =================
                using (SqlCommand cmdSlider = new SqlCommand("Redirection_TAB_SP", con))
                {
                    cmdSlider.CommandType = CommandType.StoredProcedure;
                    cmdSlider.Parameters.AddWithValue("@nType", 0);
                    cmdSlider.Parameters.AddWithValue("@nsType", 4);
                    cmdSlider.Parameters.AddWithValue("@CustomerId", id);
                    cmdSlider.Parameters.AddWithValue("@SliderType", "Customer");

                    using (SqlDataReader dr = await cmdSlider.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            sliders.Add(new DetailSliderModel
                            {
                                SliderId = Convert.ToInt32(dr["SliderId"]),
                                SliderName = dr["SliderName"]?.ToString(),
                                SliderMovingTimer = dr["SliderMovingTimer"] == DBNull.Value ? 5 : Convert.ToInt32(dr["SliderMovingTimer"]),
                                HeadingSlider = dr["HeadingSlider"]?.ToString(),
                                DescriptionSlider = dr["DescriptionSlider"]?.ToString()
                            });

                            subHeaderTitle = dr["SubHeader"]?.ToString();
                        }
                    }
                }

                // ================= PRODUCT DATA =================
                using (SqlCommand cmd = new SqlCommand("Redirection_TAB_SP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nType", 0);
                    cmd.Parameters.AddWithValue("@nsType", 3);
                    cmd.Parameters.AddWithValue("@CustomerId", id);

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
                                Prices = dr["Prices"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["Prices"]),
                                DiscountAmount = dr["DiscountAmount"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["DiscountAmount"]),
                                CustomerName = dr["CustomerName"]?.ToString(),
                                DeliveryCharges = dr["DeliveryCharges"] == DBNull.Value ? 0 : Convert.ToDecimal(dr["DeliveryCharges"])
                            });
                        }
                    }
                }
            }

            var viewModel = new RestaurantDetailViewModel
            {
                Sliders = sliders,
                Products = products
                    .GroupBy(p => p.Category)
                    .ToDictionary(g => g.Key, g => g.ToList()),
                RestaurantName = products.FirstOrDefault()?.CustomerName ?? "Restaurant",
                RestaurantId = id,
                SubHeaderTitle = subHeaderTitle ?? ""
            };

            return View(viewModel);
        }


        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] OrderRequest model)
        {
            // 1. Initial Validation
            if (model == null || model.Items == null || !model.Items.Any())
                return BadRequest(new MessageResponse { StatusId = 0, Message = "Invalid order data." });

            // Optional: Remove this delay in production
            await Task.Delay(2000); 

            string conStr = _config.GetConnectionString("Connection1");
            string MasterXML = string.Empty;
            string DetailXML = string.Empty;

            // 2. Build XML (This logic remains the same)
            string[] MasterXMLParameters = { "ContactNo", "CustomerName", "CustomerAddress", "Street", "Floor", "Description" };
            string[] MasterXMLValues = { model.ContactNo ?? "", model.CustomerName ?? "", model.CustomerAddress ?? "", model.Street ?? "", model.Floor ?? "", model.Description ?? "" };
            MasterXML = CreateXML(MasterXMLParameters, MasterXMLValues, "MasterXML");
            MasterXML = $"<Insert1>{MasterXML}</Insert1>";

            foreach (var i in model.Items)
            {
                string[] DetailXMLParameters = { "ProductCode", "Quantity", "Rate", "DiscountAmount" };
                string[] DetailXMLValues = { i.ProductCode ?? "", i.Quantity.ToString() ?? "0", i.Rate.ToString() ?? "0", i.DiscountAmount.ToString() ?? "0" };
                DetailXML += CreateXML(DetailXMLParameters, DetailXMLValues, "DetailXML");
            }
            DetailXML = $"<Insert1>{DetailXML}</Insert1>";

            // 3. Create an instance of the response class
            var response = new MessageResponse();

            using (SqlConnection con = new SqlConnection(conStr))
            {
                string executionLine = $@"
                EXEC EcomOrder_SP
                    @nType = 0,
                    @nsType = 0,
                    @MasterXML = '{MasterXML.Replace("'", "''")}',
                    @DetailXML = '{DetailXML.Replace("'", "''")}',
                    @DeliveryCharges = {model.Items[0].DeliveryCharges},
                    @RestaurantId = {model.RestaurantId}
                ";

                await con.OpenAsync();

                using (SqlCommand cmd = new SqlCommand(executionLine, con))
                {
                    cmd.CommandType = CommandType.Text;   // 👈 EXEC string run hogi

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            response.StatusId = reader["StatusId"] != DBNull.Value
                                ? Convert.ToInt32(reader["StatusId"])
                                : 0;

                            response.Message = reader["Message"] != DBNull.Value
                                ? reader["Message"].ToString()
                                : "";
                        }
                        else
                        {
                            response.StatusId = 0;
                            response.Message = "Order could not be placed. No response from server.";
                        }
                    }
                }
            }

            // 5. Return the MessageResponse object
            // The HTTP 200 OK status indicates the request was processed.
            // The StatusId inside the payload indicates the business logic outcome.
            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> CustomerExsistOrNo([FromBody] OrderRequest model)
        {
            if (model == null)
                return BadRequest(new { StatusId = 0, Message = "Invalid order data." });

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

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            return Ok(new
                            {
                                StatusId = Convert.ToInt32(reader["StatusId"]),
                                Message = reader["Message"].ToString(),

                                CustomerName = reader["CustomerName"].ToString(),
                                CustomerAddress = reader["CustomerAddress"].ToString(),
                                Street = reader["Street"].ToString(),
                                Floor = reader["Floor"].ToString(),
                                Description = reader["Description"].ToString()
                            });
                        }
                    }
                }
            }

            // Agar customer exist nahi karta
            return Ok(new
            {
                StatusId = 0,
                Message = "Customer not found"
            });
        }




        string CreateXML(string[] Parameters, string[] Values, string XmlTAG)
        {
            try
            {
                string val = "";
                foreach (var key in Parameters.Select((value, i) => new { i, value }))
                {
                    val += Parameters[key.i] + " = '" + Values[key.i] + "' ";
                }
                val = val.Replace("'", "\"");
                string XML = "<" + XmlTAG + " " + val + "/>";
                return XML;
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        string CreateDebugExecutionLine(SqlCommand cmd)
        {
            var sb = new StringBuilder();
            sb.Append("EXEC ").Append(cmd.CommandText).Append(" ");

            for (int i = 0; i < cmd.Parameters.Count; i++)
            {
                var p = cmd.Parameters[i];

                if (i > 0)
                    sb.Append(", ");

                string value;

                if (p.Value == null || p.Value == DBNull.Value)
                {
                    value = "NULL";
                }
                else if (
                    p.SqlDbType == SqlDbType.NVarChar ||
                    p.SqlDbType == SqlDbType.VarChar ||
                    p.SqlDbType == SqlDbType.Xml ||
                    p.SqlDbType == SqlDbType.Date ||
                    p.SqlDbType == SqlDbType.DateTime
                )
                {
                    value = $"'{p.Value.ToString().Replace("'", "''")}'";
                }
                else
                {
                    value = p.Value.ToString();
                }

                sb.Append(p.ParameterName).Append(" = ").Append(value);
            }

            return sb.ToString();
        }

    }
}
