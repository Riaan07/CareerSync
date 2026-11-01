using Microsoft.AspNetCore.Mvc;

namespace CareerSync.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
