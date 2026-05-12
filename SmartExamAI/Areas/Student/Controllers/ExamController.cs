using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartExamAI.Data;
using SmartExamAI.Models;
using SmartExamAI.ViewModels.Student;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartExamAI.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
    [Route("Student/[controller]")]
    public class ExamController : Controller
    {
        private const string McqType = "MCQ";
        private const string TrueFalseType = "TrueFalse";
        private const string ShortAnswerType = "ShortAnswer";

        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExamController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("Rules/{examId:int}")]
        public async Task<IActionResult> Rules(int examId)
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null) return Challenge();

            var exam = await _context.Exams.Include(e => e.Course).FirstOrDefaultAsync(e => e.Id == examId);
            if (exam == null) return NotFound();

            if (!exam.IsPublished)
            {
                TempData["Error"] = "This exam is not currently available.";
                return RedirectToAction("Index", "Courses", new { area = "Student" });
            }

            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.CourseId == exam.CourseId && e.StudentId == studentId);
            if (!isEnrolled)
            {
                TempData["Error"] = "You are not enrolled in this course.";
                return RedirectToAction("Index", "Courses", new { area = "Student" });
            }

            if (!SmartExamAI.Helpers.ExamStatusHelper.IsActive(exam.StartTime, exam.DurationMinutes))
            {
                TempData["Error"] = "This exam is not currently active.";
                return RedirectToAction("Details", "Courses", new { area = "Student", id = exam.CourseId });
            }

            var submission = await _context.Submissions.FirstOrDefaultAsync(s => s.ExamId == exam.Id && s.StudentId == studentId);
            if (submission != null)
            {
                if (submission.IsTerminated)
                {
                    return RedirectToAction(nameof(Terminated), new { submissionId = submission.Id });
                }
                if (submission.SubmittedAt != null)
                {
                    return RedirectToAction(nameof(Result), new { submissionId = submission.Id });
                }
            }

            var model = new ExamRulesViewModel
            {
                ExamId = exam.Id,
                Title = exam.Title,
                DurationMinutes = exam.DurationMinutes,
                ViolationThreshold = exam.ViolationThreshold,
                QuestionRandomization = exam.QuestionRandomization,
                CourseName = exam.Course.Title
            };

            return View(model);
        }

        [HttpGet("Take/{examId:int}")]
        public async Task<IActionResult> Take(int examId)
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null)
            {
                return Challenge();
            }

            var exam = await _context.Exams
                .Include(e => e.Course)
                .Include(e => e.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(e => e.Id == examId);

            // 1. Check exam exists -> 404 if not
            if (exam == null)
            {
                return NotFound();
            }

            // 2. Check exam.IsPublished == true -> redirect to student courses with error
            if (!exam.IsPublished)
            {
                TempData["Error"] = "This exam is not currently available.";
                return RedirectToAction("Index", "Courses", new { area = "Student" });
            }

            // 3. Check student is enrolled in exam's course -> redirect with error
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseId == exam.CourseId && e.StudentId == studentId);

            if (!isEnrolled)
            {
                TempData["Error"] = "You are not enrolled in this course.";
                return RedirectToAction("Index", "Courses", new { area = "Student" });
            }

            // 4. Check time window
            var endTime = SmartExamAI.Helpers.ExamStatusHelper.GetEndTime(exam.StartTime, exam.DurationMinutes);

            if (!SmartExamAI.Helpers.ExamStatusHelper.IsActive(exam.StartTime, exam.DurationMinutes))
            {
                TempData["Error"] = "This exam is not currently active.";
                return RedirectToAction("Index", "Courses", new { area = "Student" });
            }

            var submission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.ExamId == exam.Id && s.StudentId == studentId);

            if (submission != null)
            {
                // 5. Check if terminated
                if (submission.IsTerminated)
                {
                    return RedirectToAction(nameof(Terminated), new { submissionId = submission.Id });
                }

                // 6. Check if completed
                if (submission.SubmittedAt != null && !submission.IsTerminated)
                {
                    TempData["Error"] = "You have already submitted this exam.";
                    return RedirectToAction("Index", "Courses", new { area = "Student" });
                }

                // 7. In-progress -> do nothing (resume)
            }
            else
            {
                // 8. No submission -> create new
                submission = new Submission
                {
                    ExamId = exam.Id,
                    StudentId = studentId,
                    StartedAt = DateTime.UtcNow,
                    SubmittedAt = null,
                    IsTerminated = false,
                    TotalScore = 0,
                    ViolationCount = 0
                };

                _context.Submissions.Add(submission);
                await _context.SaveChangesAsync();

                var answers = exam.Questions.Select(q => new Answer
                {
                    SubmissionId = submission.Id,
                    QuestionId = q.Id,
                    SelectedOptionId = null,
                    TextAnswer = null,
                    IsCorrect = null,
                    Score = 0,
                    TeacherFeedback = null
                }).ToList();

                _context.Answers.AddRange(answers);
                await _context.SaveChangesAsync();
            }

            var questions = exam.Questions
                .OrderBy(q => q.OrderIndex)
                .ThenBy(q => q.Id)
                .ToList();

            if (exam.QuestionRandomization)
            {
                questions = ShuffleQuestions(questions, submission.Id);
            }

            var orderedQuestions = questions
                .Select((q, index) => new ExamQuestionViewModel
                {
                    QuestionId = q.Id,
                    Text = q.Text,
                    Type = q.Type,
                    Marks = q.Marks,
                    OrderIndex = index + 1,
                    Options = q.Options
                        .OrderBy(o => o.Id)
                        .Select(o => new ExamOptionViewModel
                        {
                            OptionId = o.Id,
                            Text = o.Text
                        })
                        .ToList()
                })
                .ToList();

            var remainingSeconds = Math.Max(0, (int)Math.Floor((endTime - DateTime.UtcNow).TotalSeconds));

            var model = new TakeExamViewModel
            {
                SubmissionId = submission.Id,
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                CourseTitle = exam.Course.Title,
                EndTimeUtc = endTime,
                TotalSeconds = remainingSeconds,
                ViolationThreshold = exam.ViolationThreshold,
                ViolationCount = submission.ViolationCount,
                Questions = orderedQuestions
            };

            return View(model);
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

            var submission = await _context.Submissions
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Questions)
                        .ThenInclude(q => q.Options)
                .Include(s => s.Answers)
                .FirstOrDefaultAsync(s => s.Id == model.SubmissionId && s.StudentId == studentId);

            if (submission == null)
            {
                return Json(new { success = false, message = "Submission not found." });
            }

            if (submission.SubmittedAt != null || submission.IsTerminated)
            {
                return Json(new { success = false, message = "Already submitted." });
            }

            var submittedAnswers = model.Answers
                .GroupBy(a => a.QuestionId)
                .ToDictionary(g => g.Key, g => g.Last());

            foreach (var answer in submission.Answers)
            {
                var question = submission.Exam.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                if (question == null)
                {
                    continue;
                }

                submittedAnswers.TryGetValue(question.Id, out var submittedAnswer);

                if (question.Type == ShortAnswerType)
                {
                    answer.SelectedOptionId = null;
                    answer.TextAnswer = submittedAnswer?.TextAnswer?.Trim();
                    answer.IsCorrect = null;
                    answer.Score = 0;
                    continue;
                }

                answer.TextAnswer = null;
                answer.SelectedOptionId = submittedAnswer?.SelectedOptionId;

                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                var isCorrect = correctOption != null && answer.SelectedOptionId == correctOption.Id;
                answer.IsCorrect = isCorrect;
                answer.Score = isCorrect ? question.Marks : 0;
            }

            submission.SubmittedAt = DateTime.UtcNow;
            submission.TotalScore = submission.Answers.Sum(a => a.Score);

            await _context.SaveChangesAsync();

            return Json(new { success = true, submissionId = submission.Id });
        }

        [HttpPost("RecordViolation")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordViolation([FromBody] RecordViolationViewModel model)
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

            var submission = await _context.Submissions
                .FirstOrDefaultAsync(s => s.Id == model.SubmissionId);

            if (submission == null)
            {
                return Json(new { success = false, message = "Submission not found." });
            }

            if (submission.StudentId != studentId)
            {
                return Json(new { success = false, message = "Unauthorized." });
            }

            if (submission.IsTerminated || submission.SubmittedAt != null)
            {
                return Json(new { success = false, message = "Exam already ended." });
            }

            var violation = new Violation
            {
                SubmissionId = submission.Id,
                Type = model.Type ?? string.Empty,
                OccurredAt = DateTime.UtcNow
            };

            _context.Violations.Add(violation);
            submission.ViolationCount++;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                violationCount = submission.ViolationCount
            });
        }

        [HttpGet("Terminated/{submissionId:int}")]
        public async Task<IActionResult> Terminated(int submissionId)
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null)
            {
                return Challenge();
            }

            var submission = await _context.Submissions
                .AsNoTracking()
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(s => s.Id == submissionId && s.StudentId == studentId);

            if (submission == null)
            {
                return NotFound();
            }

            return View(submission);
        }

        [HttpGet("Result/{submissionId:int}")]
        public async Task<IActionResult> Result(int submissionId)
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null)
            {
                return Challenge();
            }

            var submission = await _context.Submissions
                .AsNoTracking()
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Course)
                .Include(s => s.Answers)
                    .ThenInclude(a => a.Question)
                        .ThenInclude(q => q.Options)
                .Include(s => s.Answers)
                    .ThenInclude(a => a.SelectedOption)
                .FirstOrDefaultAsync(s => s.Id == submissionId && s.StudentId == studentId);

            if (submission == null)
            {
                return NotFound();
            }

            var maxScore = await _context.Questions
                .AsNoTracking()
                .Where(q => q.ExamId == submission.ExamId)
                .SumAsync(q => (int?)q.Marks) ?? 0;

            var percentage = maxScore == 0
                ? 0
                : Math.Round((decimal)submission.TotalScore / maxScore * 100, 2);

            var model = new ExamResultDetailViewModel
            {
                SubmissionId = submission.Id,
                ExamTitle = submission.Exam.Title,
                CourseTitle = submission.Exam.Course.Title,
                SubmittedAt = submission.SubmittedAt ?? DateTime.UtcNow,
                IsTerminated = submission.IsTerminated,
                ResultsPublished = submission.Exam.ResultsPublished,
                TotalScore = submission.TotalScore,
                MaxScore = maxScore,
                Percentage = percentage,
                Answers = submission.Answers
                    .OrderBy(a => a.Question.OrderIndex)
                    .Select(a => new StudentAnswerResultViewModel
                    {
                        QuestionId = a.QuestionId,
                        QuestionText = a.Question.Text,
                        QuestionType = a.Question.Type,
                        Marks = a.Question.Marks,
                        OrderIndex = a.Question.OrderIndex,
                        SelectedOptionText = a.SelectedOption?.Text,
                        CorrectOptionText = submission.Exam.ResultsPublished ? a.Question.Options.FirstOrDefault(o => o.IsCorrect)?.Text : null,
                        TextAnswer = a.TextAnswer,
                        IsCorrect = a.IsCorrect,
                        Score = a.Score,
                        TeacherFeedback = a.TeacherFeedback,
                        IsPending = a.IsCorrect == null && a.Question.Type == ShortAnswerType,
                        ResultsPublished = submission.Exam.ResultsPublished
                    })
                    .ToList()
            };

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

            var answer = await _context.Answers
                .Include(a => a.Submission)
                .FirstOrDefaultAsync(a =>
                    a.SubmissionId == model.SubmissionId &&
                    a.QuestionId == model.QuestionId &&
                    a.Submission.StudentId == studentId);

            if (answer == null)
            {
                return Json(new { success = false, message = "Answer not found." });
            }

            if (answer.Submission.SubmittedAt != null || answer.Submission.IsTerminated)
            {
                return Json(new { success = false, message = "Exam already ended." });
            }

            answer.SelectedOptionId = model.SelectedOptionId;
            answer.TextAnswer = model.TextAnswer;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        private static List<Question> ShuffleQuestions(List<Question> questions, int submissionId)
        {
            var random = new Random(submissionId);
            var shuffled = questions.ToList();

            for (var i = shuffled.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            return shuffled;
        }
    }
}
