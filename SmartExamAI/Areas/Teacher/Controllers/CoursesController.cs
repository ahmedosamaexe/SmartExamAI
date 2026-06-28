using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartExamAI.Models;
using SmartExamAI.Services;
using SmartExamAI.ViewModels.Teacher;
using System.Threading.Tasks;

namespace SmartExamAI.Areas.Teacher.Controllers
{
    [Area("Teacher")]
    [Authorize(Roles = "Teacher")]
    [Route("Teacher/[controller]")]
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
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var courses = await _courseService.GetTeacherCoursesAsync(teacherId);
            return View(courses);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View(new CreateCourseViewModel());
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCourseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var course = await _courseService.CreateCourseAsync(model, teacherId);
            TempData["Success"] = "Course created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var model = await _courseService.GetTeacherCourseDetailsAsync(id, teacherId);
            if (model == null) return NotFound();

            return View(model);
        }

        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var course = await _courseService.GetOwnedCourseAsync(id, teacherId);
            if (course == null) return NotFound();

            ViewData["EnrollCode"] = course.EnrollCode;
            var model = new EditCourseViewModel
            {
                Id = course.Id,
                Title = course.Title,
                EnrollCode = course.EnrollCode,
                Tagline = course.Tagline,
                Description = course.Description,
                Color = course.Color,
                Category = course.Category
            };

            return View(model);
        }

        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditCourseViewModel model)
        {
            if (id != model.Id) return BadRequest();

            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var course = await _courseService.GetOwnedCourseAsync(id, teacherId);
            if (course == null) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewData["EnrollCode"] = course.EnrollCode;
                return View(model);
            }

            await _courseService.UpdateCourseAsync(course, model);
            TempData["Success"] = "Course updated successfully.";
            return RedirectToAction(nameof(Details), new { id = course.Id });
        }

        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var course = await _courseService.GetOwnedCourseAsync(id, teacherId);
            if (course == null) return NotFound();

            await _courseService.DeleteCourseAsync(course);
            TempData["Success"] = "Course deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}/AddStudent")]
        public async Task<IActionResult> AddStudentManual(int id)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var course = await _courseService.GetOwnedCourseAsync(id, teacherId);
            if (course == null) return NotFound();

            ViewData["CourseTitle"] = course.Title;
            return View("AddStudent", new AddStudentManualViewModel { CourseId = id });
        }

        [HttpPost("{id:int}/AddStudent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudentManual(int id, AddStudentManualViewModel model)
        {
            if (id != model.CourseId) return BadRequest();

            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var course = await _courseService.GetOwnedCourseAsync(id, teacherId);
            if (course == null) return NotFound();

            ViewData["CourseTitle"] = course.Title;

            if (!ModelState.IsValid)
            {
                return View("AddStudent", model);
            }

            var result = await _courseService.EnrollOrEnsureUserAsync(model.Email, model.FullName, id);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to add this student.");
                return View("AddStudent", model);
            }

            return RedirectToAction(nameof(Details), new { id = course.Id });
        }

        [HttpPost("{id:int}/RemoveStudent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudent(int id, string studentId)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var course = await _courseService.GetOwnedCourseAsync(id, teacherId);
            if (course == null) return NotFound();

            await _courseService.RemoveStudentAsync(id, studentId);
            return RedirectToAction(nameof(Details), new { id = id });
        }
    }
}
