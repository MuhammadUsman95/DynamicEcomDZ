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
                                Type = dr["Type"]?.ToString()
                            });
                        }
                    }
                }
            }

            return View(model);
        }
    }
}
