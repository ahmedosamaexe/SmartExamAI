using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SmartExamAI.Models;
using SmartExamAI.Services;
using SmartExamAI.ViewModels.Student;
using System.Threading.Tasks;

namespace SmartExamAI.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
    [Route("Student/[controller]")]
    public class ExamController : Controller
    {
        private readonly ExamService _examService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExamController(ExamService examService, UserManager<ApplicationUser> userManager)
        {
            _examService = examService;
            _userManager = userManager;
        }

        [HttpGet("Rules/{examId:int}")]
        public async Task<IActionResult> Rules(int examId)
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null) return Challenge();

            var result = await _examService.GetStudentExamRulesAsync(examId, studentId);
            if (result.Model == null)
            {
                if (result.ErrorMessage == "TERMINATED")
                {
                    return RedirectToAction(nameof(Terminated), new { submissionId = result.RedirectCourseId });
                }
                if (result.ErrorMessage == "RESULT")
                {
                    return RedirectToAction(nameof(Result), new { submissionId = result.RedirectCourseId });
                }

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    TempData["Error"] = result.ErrorMessage;
                    if (result.RedirectCourseId > 0)
                    {
                        return RedirectToAction("Details", "Courses", new { area = "Student", id = result.RedirectCourseId });
                    }
                    return RedirectToAction("Index", "Courses", new { area = "Student" });
                }
                return NotFound();
            }

            return View(result.Model);
        }

        [HttpGet("Take/{examId:int}")]
        public async Task<IActionResult> Take(int examId)
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null) return Challenge();

            var result = await _examService.GetStudentTakeExamAsync(examId, studentId);
            if (result.Model == null)
            {
                if (result.RedirectAction == "Terminated")
                {
                    return RedirectToAction(nameof(Terminated), new { submissionId = result.RedirectSubmissionId });
                }
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    TempData["Error"] = result.ErrorMessage;
                    return RedirectToAction("Index", "Courses", new { area = "Student" });
                }
                return NotFound();
            }

            return View(result.Model);
        }

        [HttpPost("Submit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit([FromBody] SubmitExamViewModel model)
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null)
            {
                return Json(new { success = false, message = "Please sign in again." });
            }

            var result = await _examService.SubmitStudentExamAsync(model, studentId);
            if (!result.Succeeded)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new { success = true, submissionId = result.SubmissionId });
        }

        [HttpPost("RecordWarning")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordWarning([FromBody] RecordWarningViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid request." });
            }

            var studentId = _userManager.GetUserId(User);
            if (studentId == null)
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            var result = await _examService.RecordStudentWarningAsync(model.SubmissionId, studentId);
            if (!result.Succeeded)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new { success = true, warningCount = result.WarningCount, isTerminated = result.IsTerminated });
        }

        [HttpGet("Terminated/{submissionId:int}")]
        public async Task<IActionResult> Terminated(int submissionId)
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null) return Challenge();

            var submission = await _examService.GetStudentTerminatedSubmissionAsync(submissionId, studentId);
            if (submission == null) return NotFound();

            return View(submission);
        }

        [HttpGet("Result/{submissionId:int}")]
        public async Task<IActionResult> Result(int submissionId)
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null) return Challenge();

            var model = await _examService.GetStudentExamResultDetailAsync(submissionId, studentId);
            if (model == null) return NotFound();

            return View(model);
        }

        [HttpPost("SaveAnswer")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAnswer([FromBody] SaveAnswerViewModel model)
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null)
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            var result = await _examService.SaveStudentAnswerAsync(model, studentId);
            if (!result.Succeeded)
            {
                return Json(new { success = false, message = result.Message });
            }

            return Json(new { success = true });
        }
    }
}
