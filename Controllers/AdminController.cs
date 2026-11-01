using Microsoft.AspNetCore.Mvc;

namespace CareerSync.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
