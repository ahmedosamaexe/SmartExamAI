using Microsoft.AspNetCore.Identity;
using SmartExamAI.Models;
using SmartExamAI.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace SmartExamAI.Services
{
    public class AccountServiceResult
    {
        public bool Succeeded { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Role { get; set; }
    }

    public class AccountService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        public async Task<AccountServiceResult> RegisterAsync(RegisterViewModel model)
        {
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
                if (!await _roleManager.RoleExistsAsync(model.Role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.Role));
                }

                await _userManager.AddToRoleAsync(user, model.Role);
                await _signInManager.SignInAsync(user, isPersistent: false);

                return new AccountServiceResult { Succeeded = true, Role = model.Role };
            }

            var error = result.Errors.FirstOrDefault()?.Description ?? "Registration failed.";
            return new AccountServiceResult { Succeeded = false, ErrorMessage = error };
        }

        public async Task<AccountServiceResult> LoginAsync(LoginViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return new AccountServiceResult { Succeeded = false, ErrorMessage = "Invalid email or password." };
            }

            var result = await _signInManager.PasswordSignInAsync(
                user.UserName!,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return new AccountServiceResult { Succeeded = true, Role = user.Role };
            }

            return new AccountServiceResult { Succeeded = false, ErrorMessage = "Invalid email or password." };
        }

        public async Task<AccountServiceResult> UpdateNameAsync(ApplicationUser user, string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return new AccountServiceResult { Succeeded = false, ErrorMessage = "Name cannot be empty" };
            }

            user.FullName = fullName.Trim();
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                return new AccountServiceResult { Succeeded = true };
            }

            var error = result.Errors.FirstOrDefault()?.Description ?? "Failed to update name.";
            return new AccountServiceResult { Succeeded = false, ErrorMessage = error };
        }

        public async Task<AccountServiceResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword, string confirmNewPassword)
        {
            if (newPassword != confirmNewPassword)
            {
                return new AccountServiceResult { Succeeded = false, ErrorMessage = "New password and confirmation do not match." };
            }

            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                return new AccountServiceResult { Succeeded = true };
            }

            var error = result.Errors.FirstOrDefault()?.Description ?? "Failed to update password.";
            return new AccountServiceResult { Succeeded = false, ErrorMessage = error };
        }

        public async Task LogoutAsync()
        {
            await _signInManager.SignOutAsync();
        }
    }
}
