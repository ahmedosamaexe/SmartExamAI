using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartExamAI.Models;
using SmartExamAI.Services;
using SmartExamAI.ViewModels;
using System.Threading.Tasks;

namespace SmartExamAI.Controllers
{
    public class AccountController : Controller
    {
        private readonly AccountService _accountService;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(AccountService accountService, UserManager<ApplicationUser> userManager)
        {
            _accountService = accountService;
            _userManager = userManager;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectBasedOnRole();
            }
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _accountService.RegisterAsync(model);
            if (result.Succeeded)
            {
                return RedirectBasedOnRole(result.Role);
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Registration failed.");
            return View(model);
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectBasedOnRole();
            }
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _accountService.LoginAsync(model);
            if (result.Succeeded)
            {
                return RedirectBasedOnRole(result.Role);
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Invalid email or password.");
            return View(model);
        }

        // GET: /Account/Profile
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email;
            ViewBag.Role = User.IsInRole("Teacher") ? "Teacher" : "Student";
            return View();
        }

        // POST: /Account/UpdateName
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateName(string FullName)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _accountService.UpdateNameAsync(user, FullName);
            if (result.Succeeded)
            {
                TempData["NameSuccess"] = "Name updated.";
            }
            else
            {
                TempData["NameError"] = result.ErrorMessage;
            }

            return RedirectToAction("Profile");
        }

        // POST: /Account/ChangePasswordPost
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePasswordPost(string CurrentPassword, string NewPassword, string ConfirmNewPassword)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _accountService.ChangePasswordAsync(user, CurrentPassword, NewPassword, ConfirmNewPassword);
            if (result.Succeeded)
            {
                TempData["PasswordSuccess"] = "Password updated successfully.";
            }
            else
            {
                TempData["PasswordError"] = result.ErrorMessage;
            }

            return RedirectToAction("Profile");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _accountService.LogoutAsync();
            return RedirectToAction("Login", "Account");
        }

        private IActionResult RedirectBasedOnRole(string? role = null)
        {
            if (role == "Teacher" || User.IsInRole("Teacher"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Teacher" });
            }
            else if (role == "Student" || User.IsInRole("Student"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Student" });
            }
            return RedirectToAction("Login", "Account");
        }
    }
}
