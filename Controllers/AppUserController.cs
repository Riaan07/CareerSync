using Microsoft.AspNetCore.Mvc;

namespace CareerSync.Controllers
{
    public class AppUserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
       

    }
}
