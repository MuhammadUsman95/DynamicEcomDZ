using DynamicEcomDZ.Models;
using System.Data;
using System.Data.SqlClient;
using DynamicEcomDZ.Models;

namespace DynamicEcomDZ.Services
{
    public class RedirectionService
    {
        private readonly IConfiguration _config;

        public RedirectionService(IConfiguration config)
        {
            _config = config;
        }


        public async Task<List<RedirectionTAB>> GetActiveTabs()
        {
            List<RedirectionTAB> list = new();
            string conStr = _config.GetConnectionString("Connection1");

            using (SqlConnection con = new SqlConnection(conStr))
            {
                using (SqlCommand cmd = new SqlCommand("Redirection_TAB_SP", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@nType", 0);
                    cmd.Parameters.AddWithValue("@nsType", 0);

                    await con.OpenAsync();
                    SqlDataReader dr = await cmd.ExecuteReaderAsync();

                    while (await dr.ReadAsync())
                    {
                        //string rawBase64 = dr["TabIcon"].ToString();

                        //// Add prefix ONLY if it is real base64 and not already prefixed
                        //string finalBase64 = string.IsNullOrWhiteSpace(rawBase64)
                        //    ? ""
                        //    : rawBase64.StartsWith("data:image")
                        //        ? rawBase64
                        //        : "data:image/png;base64," + rawBase64;

                        list.Add(new RedirectionTAB
                        {
                            Name = dr["Name"].ToString(),
                            FormUrl = dr["FormURL"].ToString(),
                            TabIcon = dr["TabIcon"].ToString(),
                            IsActive = Convert.ToBoolean(dr["IsActive"])
                        });
                    }

                }
            }

            return list;
        }
    }
}
