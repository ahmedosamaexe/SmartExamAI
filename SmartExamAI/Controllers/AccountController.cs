using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartExamAI.Models;
using SmartExamAI.ViewModels;

namespace SmartExamAI.Controllers
{
    public class AccountController : Controller
    {
        private const string TeacherInviteCode = "SMARTEXAM2025";
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
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

            if (model.Role == "Teacher" && model.InviteCode?.Trim() != TeacherInviteCode)
            {
                ModelState.AddModelError("InviteCode", "Invalid invite code.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                FullName = model.FullName,
                UserName = model.Email,
                Email = model.Email,
                Role = model.Role
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Ensure role exists
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }

                await _userManager.AddToRoleAsync(user, model.Role);
                await _signInManager.SignInAsync(user, isPersistent: false);

                if (model.Role == "Teacher")
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Teacher" });
                }
                else
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Student" });
                }
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

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

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {

                if (user.Role == "Teacher")
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Teacher" });
                }
                else
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Student" });
                }
            }

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // GET: /Account/ChangePassword
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            ViewBag.FullName = user.FullName;
            ViewBag.Email = user.Email;

            var model = new ChangePasswordViewModel();
            return View(model);
        }

        // POST: /Account/ChangePassword
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                return RedirectBasedOnRole();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

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

            if (string.IsNullOrWhiteSpace(FullName))
            {
                TempData["NameError"] = "Name cannot be empty";
            }
            else
            {
                user.FullName = FullName.Trim();
                await _userManager.UpdateAsync(user);
                TempData["NameSuccess"] = "Name updated.";
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

            if (NewPassword != ConfirmNewPassword)
            {
                TempData["PasswordError"] = "New password and confirmation do not match.";
                return RedirectToAction("Profile");
            }

            var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                TempData["PasswordSuccess"] = "Password updated successfully.";
            }
            else
            {
                TempData["PasswordError"] = result.Errors.First().Description;
            }

            return RedirectToAction("Profile");
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        private IActionResult RedirectBasedOnRole()
        {
            if (User.IsInRole("Teacher"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Teacher" });
            }
            else if (User.IsInRole("Student"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Student" });
            }
            return RedirectToAction("Login", "Account");
        }
    }
}
