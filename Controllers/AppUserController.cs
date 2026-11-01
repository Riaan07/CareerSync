using Microsoft.AspNetCore.Mvc;

namespace CareerSync.Controllers
{
    public class AppUserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Error()
        {
            return View();
        }
        public IActionResult About()
        {
            return View();
        }
        public IActionResult FAQs()
        {
            return View();
        }

    }
}
