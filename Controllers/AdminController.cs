using Microsoft.AspNetCore.Mvc;
using Donora.Models.Repositories;
using Donora.Models.Entities;

namespace Donora.Controllers
{
    public class AdminController : Controller
    {
        private readonly InitiativeRepository _repo;

        // Repository is injected via Dependency Injection
        public AdminController(InitiativeRepository repo)
        {
            _repo = repo;
        }

        #region Dashboard & Reports

        // Entry point for the Admin Dashboard (Stats Cards)
        public IActionResult Index()
        {
            // REFACTOR: Using centralized helper for security
            if (!VerifyAdminAccess()) return RedirectToAction("Login", "Account");

            int adminId = HttpContext.Session.GetInt32("UserId")!.Value;

            // FIX: Changed _initiativeRepo to _repo to match the field declared above
            var stats = _repo.GetDashboardStats(adminId);

            return View(stats);
        }

        // Action for Global Analytics (The 6 Inquiries)
        public IActionResult Reports()
        {
            if (!VerifyAdminAccess()) return RedirectToAction("Login", "Account");

            var reportData = _repo.GetGlobalReports();
            return View(reportData);
        }

        #endregion

        #region Initiative Management (CRUD)

        public IActionResult ManageInitiatives()
        {
            if (!VerifyAdminAccess()) return RedirectToAction("Login", "Account");

            int userId = HttpContext.Session.GetInt32("UserId")!.Value;
            var myInitiatives = _repo.GetByCreatorId(userId);
            return View(myInitiatives);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!VerifyAdminAccess()) return RedirectToAction("Login", "Account");

            ViewBag.Sectors = _repo.GetAllSectors();
            return View();
        }

        [HttpPost]
        public IActionResult Create(Initiative initiative)
        {
            if (!VerifyAdminAccess()) return RedirectToAction("Login", "Account");

            initiative.CreatedByUserId = HttpContext.Session.GetInt32("UserId")!.Value;

            if (_repo.CreateInitiative(initiative))
            {
                TempData["SuccessMessage"] = "Initiative created successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.Sectors = _repo.GetAllSectors();
            return View(initiative);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!VerifyAdminAccess()) return RedirectToAction("Login", "Account");

            var initiative = _repo.GetById(id);
            if (initiative == null) return RedirectToAction("ManageInitiatives");

            ViewBag.Sectors = _repo.GetAllSectors();
            return View(initiative);
        }

        [HttpPost]
        public IActionResult Edit(Initiative initiative)
        {
            if (!VerifyAdminAccess()) return RedirectToAction("Login", "Account");

            if (_repo.UpdateInitiative(initiative))
            {
                TempData["SuccessMessage"] = "Changes saved successfully.";
                return RedirectToAction("ManageInitiatives");
            }

            ViewBag.Sectors = _repo.GetAllSectors();
            return View(initiative);
        }

        public IActionResult Delete(int id)
        {
            if (!VerifyAdminAccess()) return RedirectToAction("Login", "Account");

            var initiative = _repo.GetById(id);
            int userId = HttpContext.Session.GetInt32("UserId")!.Value;

            if (initiative != null)
            {
                // Logic Gate: Prevent deletion of projects with existing funding
                if (initiative.CurrentRaised > 0)
                {
                    TempData["ErrorMessage"] = $"Blocked: '{initiative.InitiativeName}' has funds and cannot be deleted.";
                }
                else
                {
                    _repo.DeleteInitiative(id, userId);
                    TempData["SuccessMessage"] = "Initiative deleted successfully.";
                }
            }
            return RedirectToAction("ManageInitiatives");
        }

        #endregion

        #region Financial Logging

        [HttpGet]
        public IActionResult LogExpense(int initiativeId)
        {
            if (!VerifyAdminAccess()) return RedirectToAction("Login", "Account");

            var initiative = _repo.GetById(initiativeId);
            if (initiative == null) return RedirectToAction("ManageInitiatives");

            decimal totalSpent = _repo.GetTotalSpent(initiativeId);
            decimal balance = initiative.CurrentRaised - totalSpent;

            ViewBag.InitiativeId = initiativeId;
            ViewBag.AvailableBalance = balance;
            ViewBag.ProjectName = initiative.InitiativeName;

            return View(new Expenditure { InitiativeId = initiativeId, DateSpent = DateTime.Now });
        }

        [HttpPost]
        public IActionResult LogExpense(Expenditure exp)
        {
            if (!VerifyAdminAccess()) return RedirectToAction("Login", "Account");

            var initiative = _repo.GetById(exp.InitiativeId);
            decimal totalSpent = _repo.GetTotalSpent(exp.InitiativeId);
            decimal balance = initiative.CurrentRaised - totalSpent;

            if (exp.AmountSpent > balance)
            {
                TempData["ErrorMessage"] = $"Insufficient Funds! Available: {balance:C}.";
                ViewBag.InitiativeId = exp.InitiativeId;
                ViewBag.AvailableBalance = balance;
                ViewBag.ProjectName = initiative?.InitiativeName;
                return View(exp);
            }

            if (exp.AmountSpent > 0 && _repo.LogExpenditure(exp))
                TempData["SuccessMessage"] = "Expenditure logged successfully.";

            return RedirectToAction("ManageInitiatives");
        }

        #endregion

        #region Helpers

        // Centralized check for Admin role and Session validity
        private bool VerifyAdminAccess()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin" &&
                   HttpContext.Session.GetInt32("UserId") != null;
        }

        #endregion
    }
}