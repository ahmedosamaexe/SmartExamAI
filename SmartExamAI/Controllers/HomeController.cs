using Microsoft.AspNetCore.Mvc;

namespace SmartExamAI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            if (User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            if (User.IsInRole("Teacher"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Teacher" });
            }

            if (User.IsInRole("Student"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Student" });
            }

            return RedirectToAction("Login", "Account");
        }
    }
}
