using Microsoft.AspNetCore.Mvc;

namespace CampaignManagement.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}