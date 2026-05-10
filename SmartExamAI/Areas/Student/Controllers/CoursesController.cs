using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartExamAI.Data;
using SmartExamAI.Models;
using SmartExamAI.ViewModels.Student;

namespace SmartExamAI.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
    [Route("Student/[controller]")]
    public class CoursesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CoursesController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null)
            {
                return Challenge();
            }

            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Course)
                    .ThenInclude(c => c.Teacher)
                .Include(e => e.Course)
                    .ThenInclude(c => c.Exams)
                .Where(e => e.StudentId == studentId)
                .OrderBy(e => e.Course.Title)
                .ToListAsync();

            var courseIds = enrollments.Select(e => e.CourseId).ToList();
            var submissions = await _context.Submissions
                .AsNoTracking()
                .Include(s => s.Exam)
                .Where(s => s.StudentId == studentId && courseIds.Contains(s.Exam.CourseId))
                .ToListAsync();

            var now = DateTime.UtcNow;

            var model = enrollments.Select(e => 
            {
                var publishedExams = e.Course.Exams.Where(ex => ex.IsPublished).ToList();
                int examCount = publishedExams.Count;
                int completedCount = submissions.Count(s => s.Exam.CourseId == e.CourseId && (s.SubmittedAt != null || s.IsTerminated));
                double percent = examCount > 0 ? ((double)completedCount / examCount) * 100 : 0;
                
                bool hasActive = publishedExams.Any(ex => SmartExamAI.Helpers.ExamStatusHelper.IsActive(ex.StartTime, ex.DurationMinutes));

                return new EnrolledCourseViewModel
                {
                    CourseId = e.Course.Id,
                    Title = e.Course.Title,
                    Tagline = e.Course.Tagline ?? string.Empty,
                    Color = e.Course.Color,
                    Category = e.Course.Category,
                    TeacherName = e.Course.Teacher.FullName,
                    ExamCount = examCount,
                    CompletedExamsPercent = percent,
                    HasActiveExam = hasActive
                };
            }).ToList();

            return View(model);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var studentId = _userManager.GetUserId(User);
            if (studentId == null)
            {
                return Challenge();
            }

            var enrollment = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Course)
                    .ThenInclude(c => c.Teacher)
                .FirstOrDefaultAsync(e => e.CourseId == id && e.StudentId == studentId);

            if (enrollment == null)
            {
                TempData["ErrorMessage"] = "You are not enrolled in this course.";
                return RedirectToAction(nameof(Index));
            }

            var exams = await _context.Exams
                .AsNoTracking()
                .Where(e => e.CourseId == id && e.IsPublished)
                .OrderBy(e => e.StartTime)
                .ToListAsync();

            var submissions = await _context.Submissions
                .Where(s => s.StudentId == studentId && exams.Select(e => e.Id).Contains(s.ExamId))
                .ToListAsync();

            var now = DateTime.UtcNow;
            var publishedExams = exams.Select(e => 
            {
                var submission = submissions.FirstOrDefault(s => s.ExamId == e.Id);
                return new StudentExamRowViewModel
                {
                    ExamId = e.Id,
                    Title = e.Title,
                    StartTime = e.StartTime,
                    DurationMinutes = e.DurationMinutes,
                    Status = SmartExamAI.Helpers.ExamStatusHelper.GetStatus(e.StartTime, e.DurationMinutes),
                    SubmissionId = submission?.Id,
                    HasResult = submission != null && (e.ResultsPublished || submission.IsTerminated)
                };
            }).ToList();

            var model = new StudentCourseDetailsViewModel
            {
                CourseId = enrollment.Course.Id,
                Title = enrollment.Course.Title,
                Tagline = enrollment.Course.Tagline ?? string.Empty,
                Color = enrollment.Course.Color,
                Category = enrollment.Course.Category,
                TeacherName = enrollment.Course.Teacher.FullName,
                PublishedExams = publishedExams
            };

            return View(model);
        }

        [HttpPost("Enroll")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enroll([FromForm] string enrollCode)
        {
            if (string.IsNullOrWhiteSpace(enrollCode))
            {
                return Json(new { success = false, message = "Please enter an enrollment code." });
            }

            var studentId = _userManager.GetUserId(User);
            if (studentId == null)
            {
                return Json(new { success = false, message = "Please sign in again and try enrolling." });
            }

            var normalizedCode = enrollCode.Trim().ToUpperInvariant();
            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.EnrollCode.ToUpper() == normalizedCode);

            if (course == null)
            {
                return Json(new { success = false, message = "Invalid enrollment code." });
            }

            var isAlreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseId == course.Id && e.StudentId == studentId);

            if (isAlreadyEnrolled)
            {
                return Json(new { success = false, message = "You are already enrolled in this course." });
            }

            _context.Enrollments.Add(new Enrollment
            {
                CourseId = course.Id,
                StudentId = studentId,
                EnrolledAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = "You have joined the course successfully.";
            return Json(new { success = true });
        }


    }
}
