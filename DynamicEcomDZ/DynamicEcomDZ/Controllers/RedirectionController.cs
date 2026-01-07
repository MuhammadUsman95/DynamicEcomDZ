using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;
using DynamicEcomDZ.Models;
using DynamicEcomDZ.Services;

namespace DynamicEcomDZ.Controllers
{
    public class RedirectionController : Controller
    {
        private readonly RedirectionService _service;
        private readonly IConfiguration _config;
        public RedirectionController(RedirectionService service, IConfiguration config)
        {
            _service = service;
            _config = config;
        }
        public async Task<IActionResult> RedirectionView()
        {
            var data = await _service.GetActiveTabs();
            return View(data);
            //return View();
        }
        public IActionResult ComingSoonPage()
        {
            return View();
        }
    }
}

