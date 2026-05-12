using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SmartExamAI.Data;
using SmartExamAI.Models;
using SmartExamAI.ViewModels.Teacher;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartExamAI.Areas.Teacher.Controllers
{
    [Area("Teacher")]
    [Authorize(Roles = "Teacher")]
    [Route("Teacher/[controller]")]
    public class ResultsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SmartExamAI.Services.NotificationService _notificationService;
        private readonly SmartExamAI.Services.IAiService _aiService;

        public ResultsController(AppDbContext context, UserManager<ApplicationUser> userManager, SmartExamAI.Services.NotificationService notificationService, SmartExamAI.Services.IAiService aiService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _aiService = aiService;
        }

        [HttpGet("GradeExam/{examId:int}")]
        public async Task<IActionResult> GradeExam(int examId)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var exam = await _context.Exams
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null || exam.Course.TeacherId != teacherId)
                return NotFound();

            var enrolledStudents = await _context.Enrollments
                .Include(e => e.Student)
                .Where(e => e.CourseId == exam.CourseId)
                .Select(e => e.Student)
                .ToListAsync();

            var submissions = await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Answers)
                    .ThenInclude(a => a.Question)
                .Where(s => s.ExamId == examId && (s.SubmittedAt != null || s.IsTerminated))
                .ToListAsync();

            var maxScore = await _context.Questions
                .Where(q => q.ExamId == examId)
                .SumAsync(q => (int?)q.Marks) ?? 0;

            var submissionRows = enrolledStudents.Select(student =>
            {
                var s = submissions.FirstOrDefault(sub => sub.StudentId == student.Id);
                if (s != null)
                {
                    var isPending = s.Answers.Any(a => a.Question.Type == "ShortAnswer" && a.IsCorrect == null);
                    return new SubmissionRowViewModel
                    {
                        SubmissionId = s.Id,
                        StudentName = s.Student.FullName,
                        StudentEmail = s.Student.Email ?? "",
                        SubmittedAt = s.SubmittedAt,
                        TotalScore = s.TotalScore,
                        MaxScore = maxScore,
                        ViolationCount = s.ViolationCount,
                        IsTerminated = s.IsTerminated,
                        GradingStatus = isPending ? "Pending" : "Done",
                        IsAbsent = false
                    };
                }
                else
                {
                    return new SubmissionRowViewModel
                    {
                        SubmissionId = 0,
                        StudentName = student.FullName,
                        StudentEmail = student.Email ?? "",
                        SubmittedAt = null,
                        TotalScore = 0,
                        MaxScore = maxScore,
                        ViolationCount = 0,
                        IsTerminated = false,
                        GradingStatus = "Done",
                        IsAbsent = true
                    };
                }
            }).OrderBy(s => s.StudentName).ToList();

            var totalRealSubmissions = submissionRows.Count(s => !s.IsAbsent);
            var gradedRealSubmissions = submissionRows.Count(s => !s.IsAbsent && s.GradingStatus == "Done");

            var model = new GradeExamViewModel
            {
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                CourseId = exam.CourseId,
                CourseTitle = exam.Course.Title,
                TotalSubmissions = totalRealSubmissions,
                GradedSubmissions = gradedRealSubmissions,
                ResultsPublished = exam.ResultsPublished,
                Submissions = submissionRows
            };

            model.AllGraded = model.TotalSubmissions > 0 && model.GradedSubmissions == model.TotalSubmissions;

            return View(model);
        }

        [HttpGet("GradeSubmission/{id:int}")]
        public async Task<IActionResult> GradeSubmission(int id)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var submission = await _context.Submissions
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Course)
                .Include(s => s.Student)
                .Include(s => s.Answers)
                    .ThenInclude(a => a.Question)
                        .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (submission == null || submission.Exam.Course.TeacherId != teacherId)
                return NotFound();

            var maxScore = await _context.Questions
                .Where(q => q.ExamId == submission.ExamId)
                .SumAsync(q => (int?)q.Marks) ?? 0;

            var answerViewModels = submission.Answers
                .OrderBy(a => a.Question.OrderIndex)
                .Select(a =>
                {
                    var selectedOption = a.SelectedOptionId.HasValue 
                        ? a.Question.Options.FirstOrDefault(o => o.Id == a.SelectedOptionId) 
                        : null;
                        
                    var correctOption = a.Question.Options.FirstOrDefault(o => o.IsCorrect);

                    return new GradeAnswerViewModel
                    {
                        AnswerId = a.Id,
                        QuestionId = a.QuestionId,
                        QuestionText = a.Question.Text,
                        QuestionType = a.Question.Type,
                        Marks = a.Question.Marks,
                        OrderIndex = a.Question.OrderIndex,
                        SelectedOptionText = selectedOption?.Text,
                        CorrectOptionText = correctOption?.Text,
                        TextAnswer = a.TextAnswer,
                        IsCorrect = a.IsCorrect,
                        Score = a.Score,
                        TeacherFeedback = a.TeacherFeedback
                    };
                }).ToList();

            var model = new GradeSubmissionViewModel
            {
                SubmissionId = submission.Id,
                ExamId = submission.ExamId,
                ExamTitle = submission.Exam.Title,
                StudentName = submission.Student.FullName,
                StudentEmail = submission.Student.Email ?? "",
                SubmittedAt = submission.SubmittedAt ?? submission.StartedAt,
                TotalScore = submission.TotalScore,
                MaxScore = maxScore,
                IsTerminated = submission.IsTerminated,
                Answers = answerViewModels
            };

            return View(model);
        }

        [HttpPost("GradeAnswer")]
        public async Task<IActionResult> GradeAnswer([FromBody] SaveGradeViewModel model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Invalid data." });

            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Json(new { success = false, message = "Unauthorized." });

            var answer = await _context.Answers
                .Include(a => a.Submission)
                    .ThenInclude(s => s.Exam)
                        .ThenInclude(e => e.Course)
                .Include(a => a.Question)
                .FirstOrDefaultAsync(a => a.Id == model.AnswerId);

            if (answer == null || answer.Submission.Exam.Course.TeacherId != teacherId)
                return Json(new { success = false, message = "Answer not found or unauthorized." });

            if (model.Score > answer.Question.Marks)
                return Json(new { success = false, message = $"Score cannot exceed maximum marks ({answer.Question.Marks})." });

            answer.Score = model.Score;
            answer.TeacherFeedback = model.TeacherFeedback;
            answer.IsCorrect = model.Score > 0 ? true : (model.Score == 0 ? false : null);

            var submission = answer.Submission;
            var allAnswers = await _context.Answers.Where(a => a.SubmissionId == submission.Id).ToListAsync();
            submission.TotalScore = allAnswers.Where(a => a.Id != answer.Id).Sum(a => a.Score) + answer.Score;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                newTotalScore = submission.TotalScore,
                answerId = answer.Id,
                score = answer.Score
            });
        }

        [HttpPost("PublishResults/{examId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishResults(int examId)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var exam = await _context.Exams
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null || exam.Course.TeacherId != teacherId)
                return NotFound();

            var submissions = await _context.Submissions
                .Include(s => s.Answers)
                    .ThenInclude(a => a.Question)
                .Where(s => s.ExamId == examId && (s.SubmittedAt != null || s.IsTerminated))
                .ToListAsync();

            bool allGraded = !submissions.Any(s => s.Answers.Any(a => a.Question.Type == "ShortAnswer" && a.IsCorrect == null));

            if (!allGraded && submissions.Any())
            {
                TempData["ErrorMessage"] = "Cannot publish results — some submissions are not fully graded yet.";
                return RedirectToAction(nameof(GradeExam), new { examId = exam.Id });
            }

            foreach (var s in submissions)
            {
                s.TotalScore = s.Answers.Sum(a => a.Score);
            }

            exam.ResultsPublished = true;
            await _context.SaveChangesAsync();

            // Notify students that results are published
            var studentIds = submissions.Select(s => s.StudentId).Distinct().ToList();
            if (studentIds.Count > 0)
            {
                await _notificationService.NotifyManyAsync(
                    studentIds,
                    "Results Published",
                    $"Results for {exam.Title} are now available.",
                    "ResultPublished",
                    $"/Student/Courses/{exam.CourseId}"
                );
            }

            TempData["Success"] = "Results are now visible to students.";
            return RedirectToAction("Details", "Courses", new { area = "Teacher", id = exam.CourseId });
        }

        [HttpPost("BulkGradeSubmission")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkGradeSubmission([FromBody] BulkGradeViewModel model)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null)
                return Json(new { success = false, message = "Unauthorized." });

            var submission = await _context.Submissions
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Course)
                .Include(s => s.Answers)
                    .ThenInclude(a => a.Question)
                .FirstOrDefaultAsync(s => s.Id == model.SubmissionId);

            if (submission == null || submission.Exam.Course.TeacherId != teacherId)
                return Json(new { success = false, message = "Submission not found or unauthorized." });

            foreach (var grade in model.Grades)
            {
                var answer = submission.Answers.FirstOrDefault(a => a.Id == grade.AnswerId);
                if (answer == null) continue;

                if (grade.Score > answer.Question.Marks)
                    return Json(new { success = false, message = $"Score for Q{answer.Question.OrderIndex} cannot exceed {answer.Question.Marks}." });

                answer.Score = grade.Score;
                answer.TeacherFeedback = grade.TeacherFeedback;
                answer.IsCorrect = grade.Score > 0 ? true : false;
            }

            submission.TotalScore = submission.Answers.Sum(a => a.Score);
            await _context.SaveChangesAsync();

            return Json(new { success = true, newTotalScore = submission.TotalScore });
        }

        [HttpGet("ExportResults/{examId:int}")]
        public async Task<IActionResult> ExportResults(int examId)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var exam = await _context.Exams
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null || exam.Course.TeacherId != teacherId)
                return NotFound();

            if (!exam.ResultsPublished)
            {
                TempData["ErrorMessage"] = "Results must be published before exporting.";
                return RedirectToAction(nameof(GradeExam), new { examId = exam.Id });
            }

            var submissions = await _context.Submissions
                .Include(s => s.Student)
                .Where(s => s.ExamId == examId && (s.SubmittedAt != null || s.IsTerminated))
                .OrderBy(s => s.Student.FullName)
                .ToListAsync();

            var maxScore = await _context.Questions
                .Where(q => q.ExamId == examId)
                .SumAsync(q => (int?)q.Marks) ?? 0;

            ExcelPackage.License.SetNonCommercialPersonal("SmartExamAI");
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Results");

            var headers = new[] { "Student Name", "Email", "Started At", "Submitted At", "Total Score", "Max Score", "Percentage", "Violations", "Status" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            using (var range = worksheet.Cells[1, 1, 1, headers.Length])
            {
                range.Style.Font.Bold = true;
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#2C2C2C"));
            }

            int row = 2;
            foreach (var s in submissions)
            {
                worksheet.Cells[row, 1].Value = s.Student.FullName;
                worksheet.Cells[row, 2].Value = s.Student.Email;
                worksheet.Cells[row, 3].Value = s.StartedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cells[row, 4].Value = s.SubmittedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
                worksheet.Cells[row, 5].Value = s.TotalScore;
                worksheet.Cells[row, 6].Value = maxScore;
                
                var pct = maxScore > 0 ? (decimal)s.TotalScore / maxScore : 0;
                worksheet.Cells[row, 7].Value = pct;
                worksheet.Cells[row, 7].Style.Numberformat.Format = "0.0%";
                
                worksheet.Cells[row, 8].Value = s.ViolationCount;
                worksheet.Cells[row, 9].Value = s.IsTerminated ? "Terminated" : "Submitted";

                if (row % 2 != 0)
                {
                    using (var r = worksheet.Cells[row, 1, row, headers.Length])
                    {
                        r.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#F5F4F0"));
                    }
                }

                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var safeTitle = new string(exam.Title.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            var fileName = $"{safeTitle}_Results.xlsx";

            return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet("Violations/{examId:int}")]
        public async Task<IActionResult> ViolationsMonitor(int examId)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var exam = await _context.Exams
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null || exam.Course.TeacherId != teacherId)
                return NotFound();

            var model = new ViolationMonitorViewModel
            {
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                CourseId = exam.CourseId
            };

            return View(model);
        }

        [HttpGet("ViolationsData/{examId:int}")]
        public async Task<IActionResult> ViolationsData(int examId)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Json(new { });

            var exam = await _context.Exams
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null || exam.Course.TeacherId != teacherId)
                return Json(new { });

            var submissions = await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Violations)
                .Where(s => s.ExamId == examId)
                .ToListAsync();

            var model = submissions.Select(s => new ViolationStudentViewModel
            {
                SubmissionId = s.Id,
                StudentName = s.Student.FullName,
                ViolationCount = s.ViolationCount,
                IsTerminated = s.IsTerminated,
                Violations = s.Violations
                    .OrderBy(v => v.OccurredAt)
                    .Select(v => new ViolationDetailViewModel
                    {
                        Type = v.Type,
                        OccurredAt = v.OccurredAt
                    }).ToList()
            }).OrderByDescending(s => s.ViolationCount).ToList();

            var totalEnrolled = await _context.Enrollments.CountAsync(e => e.CourseId == exam.CourseId);
            var completedCount = submissions.Count(s => s.SubmittedAt != null || s.IsTerminated);
            bool allSubmitted = totalEnrolled > 0 && completedCount >= totalEnrolled;

            return Json(new { allSubmitted = allSubmitted, data = model });
        }

        // ── AI Grading ──

        [HttpPost("SuggestGrade")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuggestGrade([FromBody] SuggestGradeRequest request)
        {
            if (!_aiService.IsEnabled)
                return Json(new { success = false, message = "AI is not configured." });

            var suggestion = await _aiService.SuggestGradeAsync(request.QuestionText, request.StudentAnswer, request.MaxMarks);
            if (suggestion == null)
                return Json(new { success = false, message = "AI could not generate a suggestion." });

            return Json(new { success = true, suggestion });
        }

        public sealed class SuggestGradeRequest
        {
            public string QuestionText { get; set; } = string.Empty;
            public string StudentAnswer { get; set; } = string.Empty;
            public int MaxMarks { get; set; }
        }
    }
}
