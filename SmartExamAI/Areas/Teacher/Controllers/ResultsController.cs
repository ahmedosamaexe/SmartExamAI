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
    public class ResultsController : Controller
    {
        private readonly ExamService _examService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ResultsController(ExamService examService, UserManager<ApplicationUser> userManager)
        {
            _examService = examService;
            _userManager = userManager;
        }

        [HttpGet("GradeExam/{examId:int}")]
        public async Task<IActionResult> GradeExam(int examId)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var model = await _examService.GetGradeExamViewModelAsync(examId, teacherId);
            if (model == null) return NotFound();

            return View(model);
        }

        [HttpGet("GradeSubmission/{id:int}")]
        public async Task<IActionResult> GradeSubmission(int id)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var model = await _examService.GetGradeSubmissionViewModelAsync(id, teacherId);
            if (model == null) return NotFound();

            return View(model);
        }

        [HttpPost("GradeAnswer")]
        public async Task<IActionResult> GradeAnswer([FromBody] SaveGradeViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data." });

            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Json(new { success = false, message = "Unauthorized." });

            var result = await _examService.GradeAnswerAsync(model, teacherId);
            if (!result.Succeeded)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new
            {
                success = true,
                newTotalScore = result.NewTotalScore,
                answerId = result.AnswerId,
                score = result.Score
            });
        }

        [HttpPost("PublishResults/{examId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishResults(int examId)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var result = await _examService.PublishResultsAsync(examId, teacherId);
            if (!result.Succeeded)
            {
                if (result.CourseId > 0)
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction(nameof(GradeExam), new { examId });
                }
                return NotFound();
            }

            TempData["Success"] = result.Message;
            return RedirectToAction("Details", "Courses", new { area = "Teacher", id = result.CourseId });
        }

        [HttpPost("BulkGradeSubmission")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkGradeSubmission([FromBody] BulkGradeViewModel model)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Json(new { success = false, message = "Unauthorized." });

            var result = await _examService.BulkGradeSubmissionAsync(model, teacherId);
            if (!result.Succeeded)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new { success = true, newTotalScore = result.NewTotalScore });
        }

        [HttpGet("ExportResults/{examId:int}")]
        public async Task<IActionResult> ExportResults(int examId)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var result = await _examService.ExportResultsExcelAsync(examId, teacherId);
            if (result.FileBytes == null)
            {
                if (!string.IsNullOrEmpty(result.ErrorMessage) && result.CourseId > 0)
                {
                    TempData["ErrorMessage"] = result.ErrorMessage;
                    return RedirectToAction(nameof(GradeExam), new { examId });
                }
                return NotFound();
            }

            return File(result.FileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName!);
        }
    }
}
