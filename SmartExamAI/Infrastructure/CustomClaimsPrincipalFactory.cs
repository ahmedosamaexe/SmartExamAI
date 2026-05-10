using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SmartExamAI.Models;

namespace SmartExamAI.Infrastructure
{
    public class CustomClaimsPrincipalFactory
        : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole>
    {
        public CustomClaimsPrincipalFactory(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IOptions<IdentityOptions> options)
            : base(userManager, roleManager, options)
        {
        }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            identity.AddClaim(new Claim("FullName", user.FullName));
            
            var isDefaultPassword = await UserManager.CheckPasswordAsync(user, "Pass1234");
            if (isDefaultPassword)
            {
                identity.AddClaim(new Claim("MustChangePassword", "true"));
            }

            return identity;
        }
    }
}
