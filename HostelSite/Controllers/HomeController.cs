//using Microsoft.AspNetCore.Mvc;

//namespace HostelSite.Controllers
//{
//    public class HomeController : Controller
//    {
//        public IActionResult Index()
//        {
//            return View();
//        }
//    }
//}
using Microsoft.AspNetCore.Mvc;

namespace HostelSite.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new HostelSite.Models.ErrorViewModel
            {
                RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}