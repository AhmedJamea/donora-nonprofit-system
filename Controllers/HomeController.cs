using Microsoft.AspNetCore.Mvc;

namespace Donora.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");

            // Direct traffic based on role
            if (role == "Admin") return RedirectToAction("Index", "Admin");
            if (role == "Supporter") return RedirectToAction("Index", "Supporter");

            // Guests see the portal page
            return View();
        }
    }
}