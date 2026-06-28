using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartExamAI.Models;
using SmartExamAI.Services;
using System.Linq;
using System.Threading.Tasks;

namespace SmartExamAI.Areas.Teacher.Controllers
{
    [Area("Teacher")]
    [Authorize(Roles = "Teacher")]
    [Route("Teacher/[controller]")]
    public class DashboardController : Controller
    {
        private readonly DashboardService _dashboardService;
        private readonly CourseService _courseService;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(DashboardService dashboardService, CourseService courseService, UserManager<ApplicationUser> userManager)
        {
            _dashboardService = dashboardService;
            _courseService = courseService;
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var model = await _dashboardService.GetTeacherDashboardDataAsync(teacherId);

            var courses = (await _courseService.GetTeacherCoursesAsync(teacherId)).ToList();
            ViewData["RecentCourses"] = courses.Take(4).ToList();
            ViewData["TotalCoursesCount"] = courses.Count;

            return View(model);
        }
    }
}
