using Microsoft.AspNetCore.Mvc;
using Donora.Models.Repositories;
using Donora.Models.Entities;

namespace Donora.Controllers
{
    public class SupporterController : Controller
    {
        private readonly ContributionRepository _contribRepo;
        private readonly InitiativeRepository _initRepo;

        public SupporterController(ContributionRepository contribRepo, InitiativeRepository initRepo)
        {
            _contribRepo = contribRepo;
            _initRepo = initRepo;
        }

        public IActionResult Index()
        {
            // Fetch data for the browse view
            ViewBag.Sectors = _initRepo.GetAllSectors();
            ViewBag.Initiatives = _initRepo.GetAllInitiativesWithProgress();

            return View();
        }

        [HttpGet]
        public IActionResult Donate(int initiativeId)
        {
            if (!VerifySupporterAccess()) return RedirectToAction("Login", "Account");

            ViewBag.InitiativeId = initiativeId;
            return View();
        }

        [HttpPost]
        public IActionResult Donate(Contribution contribution)
        {
            contribution.SupporterId = HttpContext.Session.GetInt32("UserId") ?? 0;

            if (_contribRepo.AddContribution(contribution))
                return RedirectToAction("Index");

            ViewBag.InitiativeId = contribution.InitiativeId;
            return View(contribution);
        }

        public IActionResult MyContributions()
        {
            if (!VerifySupporterAccess()) return RedirectToAction("Login", "Account");

            int userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var history = _contribRepo.GetHistoryBySupporter(userId);

            return View(history);
        }

        // REFACTOR: Centralized security check for Supporters
        private bool VerifySupporterAccess()
        {
            return HttpContext.Session.GetString("UserRole") == "Supporter" &&
                   HttpContext.Session.GetInt32("UserId") != null;
        }
    }
}