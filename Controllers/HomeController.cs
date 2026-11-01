using Microsoft.AspNetCore.Mvc;

namespace CareerSync.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
