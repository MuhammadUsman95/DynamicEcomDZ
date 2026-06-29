using System.Data;
using System.Data.SqlClient;

Console.Title = "Whatsapp URL Runner";

const int IntervalMinutes = 45;

string connectionString =
    @"Server=DESKTOP-QQK7C5E\SQL2016;Database=CampaignManagement;User ID=sa;Password=sql2016;TrustServerCertificate=True;";

using HttpClient httpClient = new HttpClient();
httpClient.Timeout = TimeSpan.FromSeconds(60);

Console.WriteLine("Whatsapp URL Runner Started...");
Console.WriteLine($"Every {IntervalMinutes} minutes execution will run.");

while (true)
{
    try
    {
        Console.WriteLine();
        Console.WriteLine("Running SP: " + DateTime.Now);

        DataTable dt = new DataTable();

        using (SqlConnection con = new SqlConnection(connectionString))
        {
            con.Open();

            using (SqlCommand cmd = new SqlCommand("EXEC dbo.WhatsappNumberSP @nType = 3, @nsType = 0", con))
            {
                cmd.CommandType = CommandType.Text;
                cmd.CommandTimeout = 120;

                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }
        }

        Console.WriteLine("Total URLs Found: " + dt.Rows.Count);

        foreach (DataRow row in dt.Rows)
        {
            string url = Convert.ToString(row["WhatsappUrl"]) ?? "";

            if (string.IsNullOrWhiteSpace(url))
                continue;

            try
            {
                Console.WriteLine("Running URL: " + url);

                HttpResponseMessage response = await httpClient.GetAsync(url);
                string responseText = await response.Content.ReadAsStringAsync();

                Console.WriteLine("Status: " + response.StatusCode);
                Console.WriteLine("Response: " + responseText);
            }
            catch (Exception ex)
            {
                Console.WriteLine("URL Error: " + ex.Message);
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("Main Error: " + ex.ToString());
    }

    Console.WriteLine($"Waiting {IntervalMinutes} minutes...");
    await Task.Delay(TimeSpan.FromMinutes(IntervalMinutes));
}