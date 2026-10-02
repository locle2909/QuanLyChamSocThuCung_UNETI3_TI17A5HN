using Microsoft.AspNetCore.Mvc;
using QuanLyChamSocThuCung_UNETI3_TI17A5HN.Models;
using System.Diagnostics;

namespace QuanLyChamSocThuCung_UNETI3_TI17A5HN.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
