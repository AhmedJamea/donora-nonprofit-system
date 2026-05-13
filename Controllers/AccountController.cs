using Microsoft.AspNetCore.Mvc;
using Donora.Models.Repositories;
using Donora.Models.Entities;

namespace Donora.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserRepository _userRepository;

        // REFACTOR: Repository is now injected via Dependency Injection
        public AccountController(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            AppUser? user = _userRepository.Authenticate(email, password);

            if (user != null)
            {
                // Set Session data
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserName", user.FullName);
                HttpContext.Session.SetString("UserRole", user.Role);

                // Role-based redirection
                if (user.Role == "Admin")
                    return RedirectToAction("Index", "Admin");

                return RedirectToAction("Index", "Supporter");
            }

            ViewBag.ErrorMessage = "Invalid email or password.";
            return View();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(AppUser user)
        {
            if (_userRepository.Register(user))
                return RedirectToAction("Login");

            ViewBag.ErrorMessage = "Registration failed. Email might already be in use.";
            return View();
        }
    }
}