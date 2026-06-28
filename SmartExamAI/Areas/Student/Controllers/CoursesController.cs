using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartExamAI.Models;
using SmartExamAI.Services;
using System.Threading.Tasks;

namespace SmartExamAI.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
    [Route("Student/[controller]")]
    public class CoursesController : Controller
    {
        private readonly CourseService _courseService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CoursesController(CourseService courseService, UserManager<ApplicationUser> userManager)
        {
            _courseService = courseService;
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null) return Challenge();

            var model = await _courseService.GetStudentEnrolledCoursesAsync(studentId);
            return View(model);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null) return Challenge();

            var model = await _courseService.GetStudentCourseDetailsAsync(id, studentId);
            if (model == null)
            {
                TempData["ErrorMessage"] = "You are not enrolled in this course.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpPost("Enroll")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll([FromForm] string enrollCode)
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null)
            {
                return Json(new { success = false, message = "Please sign in again and try enrolling." });
            }

            var result = await _courseService.EnrollStudentWithCodeAsync(enrollCode, studentId);
            if (!result.Success)
            {
                return Json(new { success = false, message = result.Message });
            }

            TempData["Success"] = result.Message;
            return Json(new { success = true });
        }
    }
}
