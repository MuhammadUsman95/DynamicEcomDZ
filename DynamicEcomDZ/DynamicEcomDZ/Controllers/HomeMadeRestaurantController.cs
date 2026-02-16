using DynamicEcomDZ.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using DynamicEcomDZ.Models;

namespace DynamicEcomDZ.Controllers
{
    public class HomeMadeRestaurantController : Controller
    {
        private readonly IConfiguration _config;

        public HomeMadeRestaurantController(IConfiguration config)
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

                    using (SqlDataReader drSlider = await cmdSlider.ExecuteReaderAsync())
                    {
                        while (await drSlider.ReadAsync())
                        {
                            model.Sliders.Add(new SliderModel
                            {
                                SliderId = drSlider["SliderId"] == DBNull.Value ? 0 : Convert.ToInt32(drSlider["SliderId"]),
                                SliderName = drSlider["SliderName"]?.ToString(),
                                SliderMovingTimer = drSlider["SliderMovingTimer"] == DBNull.Value ? 5 : Convert.ToInt32(drSlider["SliderMovingTimer"]),

                                // ✅ HEADING & DESCRIPTION FROM DB
                                HeadingSlider = drSlider["HeadingSlider"]?.ToString(),
                                DescriptionSlider = drSlider["DescriptionSlider"]?.ToString()
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

                    using (SqlDataReader drCustomer = await cmdCustomer.ExecuteReaderAsync())
                    {
                        while (await drCustomer.ReadAsync())
                        {
                            model.Customers.Add(new CustomerModel
                            {
                                CustomerId = drCustomer["CustomerId"] == DBNull.Value ? 0 : Convert.ToInt32(drCustomer["CustomerId"]),
                                Customer = drCustomer["Customer"]?.ToString(),
                                ImagePath = drCustomer["ImagePath"]?.ToString(),
                                Rating = drCustomer["Rating"] == DBNull.Value ? 0 : Convert.ToInt32(drCustomer["Rating"])
                            });
                        }
                    }
                }

                await con.CloseAsync();
            }

            return View(model);
        }
    }
}
