using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartExamAI.Models;
using SmartExamAI.Services;
using SmartExamAI.ViewModels.Teacher;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartExamAI.Areas.Teacher.Controllers
{
    [Area("Teacher")]
    [Authorize(Roles = "Teacher")]
    [Route("Teacher/[controller]")]
    public class ExamsController : Controller
    {
        private readonly ExamService _examService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExamsController(ExamService examService, UserManager<ApplicationUser> userManager)
        {
            _examService = examService;
            _userManager = userManager;
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create(int courseId)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var model = await _examService.GetCreateExamViewModelAsync(courseId, teacherId);
            if (model == null) return NotFound();

            var course = await _examService.GetOwnedCourseAsync(courseId, teacherId);
            ViewData["CourseTitle"] = course?.Title;

            return View(model);
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateExamViewModel model)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            if (model.StartTime == default)
            {
                ModelState.AddModelError(nameof(model.StartTime), "Start Time is required.");
            }

            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = GetModelStateErrorMessage() });
                }

                var course = await _examService.GetOwnedCourseAsync(model.CourseId, teacherId);
                ViewData["CourseTitle"] = course?.Title;
                return View(model);
            }

            var result = await _examService.CreateExamFromModelAsync(model, teacherId);
            if (!result.Succeeded || result.Exam == null)
            {
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = result.ErrorMessage });
                }
                return NotFound();
            }

            if (IsAjaxRequest())
            {
                return Json(new { success = true, examId = result.Exam.Id });
            }

            TempData["Success"] = "Exam created successfully.";
            return RedirectToAction(nameof(Details), new { id = result.Exam.Id });
        }

        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var model = await _examService.GetTeacherExamDetailsAsync(id, teacherId);
            if (model == null) return NotFound();

            return View(model);
        }

        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var result = await _examService.GetEditExamViewModelAsync(id, teacherId);
            if (result.Model == null)
            {
                if (!string.IsNullOrEmpty(result.ErrorMessage) && result.CourseId > 0)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction(nameof(Details), new { id });
                }
                return NotFound();
            }

            var course = await _examService.GetOwnedCourseAsync(result.CourseId, teacherId);
            ViewData["CourseTitle"] = course?.Title;

            return View(result.Model);
        }

        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditExamViewModel model)
        {
            if (id != model.Id) return BadRequest();

            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            if (model.StartTime == default)
            {
                ModelState.AddModelError(nameof(model.StartTime), "Start Time is required.");
            }

            if (!ModelState.IsValid)
            {
                var course = await _examService.GetOwnedCourseAsync(model.CourseId, teacherId);
                ViewData["CourseTitle"] = course?.Title;
                return View(model);
            }

            var result = await _examService.UpdateExamFromModelAsync(id, model, teacherId);
            if (!result.Succeeded)
            {
                if (result.CourseId > 0)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction(nameof(Details), new { id });
                }
                return NotFound();
            }

            TempData["Success"] = "Exam updated successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var result = await _examService.DeleteExamByIdAsync(id, teacherId);
            if (!result.Succeeded) return NotFound();

            TempData["Success"] = "Exam deleted successfully.";
            return RedirectToAction("Details", "Courses", new { area = "Teacher", id = result.CourseId });
        }

        [HttpPost("TogglePublish/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Json(new { success = false, message = "Unauthorized." });

            var result = await _examService.TogglePublishByIdAsync(id, teacherId);
            if (!result.Succeeded)
            {
                return Json(new { success = false, message = result.Message });
            }

            TempData["Success"] = result.Message;
            return Json(new { success = true, isPublished = result.IsPublished });
        }

        [HttpPost("AddQuestion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(AddQuestionViewModel model)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Json(new { success = false, message = "Unauthorized." });

            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = GetModelStateErrorMessage() });
            }

            var result = await _examService.AddQuestionToExamAsync(model, teacherId);
            if (!result.Succeeded)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new { success = true, question = result.Question });
        }

        [HttpPost("DeleteQuestion/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Json(new { success = false, message = "Unauthorized." });

            var result = await _examService.DeleteQuestionByIdAsync(id, teacherId);
            if (!result.Succeeded)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new { success = true });
        }


        private bool IsAjaxRequest()
        {
            return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }

        private string GetModelStateErrorMessage()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
                ?? "Please correct the form and try again.";
        }
    }
}
