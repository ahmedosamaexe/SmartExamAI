using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;

namespace SmartExamAI.Filters
{
    public class ForcePasswordChangeFilter : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                if (user.HasClaim(c => c.Type == "MustChangePassword" && c.Value == "true"))
                {
                    var controller = context.RouteData.Values["controller"]?.ToString();
                    var action = context.RouteData.Values["action"]?.ToString();

                    if (!(controller == "Account" && (action == "ChangePassword" || action == "ChangePasswordPost" || action == "Logout")))
                    {
                        context.Result = new RedirectToRouteResult(new RouteValueDictionary
                        {
                            { "area", "" },
                            { "controller", "Account" },
                            { "action", "ChangePassword" }
                        });
                        return;
                    }
                }
            }

            await next();
        }
    }
}
