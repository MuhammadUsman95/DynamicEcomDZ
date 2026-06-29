using CampaignManagement.Models;
using Microsoft.AspNetCore.Mvc;

namespace CampaignManagement.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (model.UserId == "admin" && model.Password == "123")
            {
                HttpContext.Session.SetString("UserId", model.UserId);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid UserId or Password";
            return View(model);
        }
    }
}