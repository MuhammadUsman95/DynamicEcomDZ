using DynamicEcomDZ.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace DynamicEcomDZ.Controllers
{
    public class RestaurantController : Controller
    {
        private readonly IConfiguration _config;

        public RestaurantController(IConfiguration config)
        {
            _config = config;
        }

        public async Task<IActionResult> Index()
        {
            string conStr = _config.GetConnectionString("Connection1");
            var model = new SliderViewModel();

            using (SqlConnection con = new SqlConnection(conStr))
            {
                await con.OpenAsync();

                // ================= SLIDER DATA =================
                using (SqlCommand cmdSlider = new SqlCommand("Redirection_TAB_SP", con))
                {
                    cmdSlider.CommandType = CommandType.StoredProcedure;
                    cmdSlider.Parameters.AddWithValue("@nType", 0);
                    cmdSlider.Parameters.AddWithValue("@nsType", 1);

                    using (SqlDataReader dr = await cmdSlider.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            model.Sliders.Add(new SliderModel
                            {
                                SilderId = Convert.ToInt32(dr["SilderId"]),
                                SilderName = dr["SilderName"]?.ToString(),
                                SliderMovingTimer = dr["SliderMovingTimer"] == DBNull.Value ? 5 : Convert.ToInt32(dr["SliderMovingTimer"]),
                                HeadingSlider = dr["HeadingSlider"]?.ToString(),
                                DescriptionSlider = dr["DescriptionSlider"]?.ToString()
                            });
                        }
                    }
                }

                // ================= CUSTOMER DATA =================
                using (SqlCommand cmdCustomer = new SqlCommand("Redirection_TAB_SP", con))
                {
                    cmdCustomer.CommandType = CommandType.StoredProcedure;
                    cmdCustomer.Parameters.AddWithValue("@nType", 0);
                    cmdCustomer.Parameters.AddWithValue("@nsType", 2);

                    using (SqlDataReader dr = await cmdCustomer.ExecuteReaderAsync())
                    {
                        while (await dr.ReadAsync())
                        {
                            model.Customers.Add(new CustomerModel
                            {
                                CustomerId = Convert.ToInt32(dr["CustomerId"]),
                                Customer = dr["Customer"]?.ToString(),
                                ImagePath = dr["ImagePath"]?.ToString(),
                                Rating = Convert.ToInt32(dr["Rating"]),
                                Timing = dr["Timing"]?.ToString(),
                                Type = dr["Type"]?.ToString(),
                                DeliveryCharges = dr["DeliveryCharges"] == DBNull.Value
                        ? 0
                        : Convert.ToDecimal(dr["DeliveryCharges"])
                            });
                        }
                    }
                }
            }

            return View(model);
        }



        [HttpPost]
        public async Task<IActionResult> CheckRestaurantValidation([FromBody] OrderRequest model)
        {
            // 1. Initial Validation
            if (model == null)
                return BadRequest(new MessageResponse { StatusId = 0, Message = "Invalid order data." });

            string conStr = _config.GetConnectionString("Connection1");
            
            // 3. Create an instance of the response class
            var response = new MessageResponse();

            // 4. Execute Database Command
            using (SqlConnection con = new SqlConnection(conStr))
            {
                await con.OpenAsync();
                using (SqlCommand cmd = new SqlCommand("Redirection_TAB_SP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add("@nType", SqlDbType.Int).Value = 0;
                    cmd.Parameters.Add("@nsType", SqlDbType.Int).Value = 5;
                    cmd.Parameters.Add("@CustomerId", SqlDbType.Int).Value = model.RestaurantId;

                    // Use ExecuteReader to get the result set
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        // Check if the SP returned a row
                        if (reader.Read())
                        {
                            // Populate the response object from the database
                            if (reader["StatusId"] != DBNull.Value)
                            {
                                response.StatusId = Convert.ToInt32(reader["StatusId"]);
                            }

                            if (reader["Message"] != DBNull.Value)
                            {
                                response.Message = reader["Message"].ToString();
                            }
                        }
                        else
                        {
                            // Handle case where SP returns no rows
                            response.StatusId = 0;
                            response.Message = "Order could not be placed. No response from server.";
                        }
                    }
                }
            }
            return Ok(response);
        }


    }
}
