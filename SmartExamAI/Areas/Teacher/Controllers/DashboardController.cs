using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartExamAI.Data;
using SmartExamAI.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SmartExamAI.Areas.Teacher.Controllers
{
    [Area("Teacher")]
    [Authorize(Roles = "Teacher")]
    [Route("Teacher/[controller]")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null) return Challenge();

            var courses = await _context.Courses.Where(c => c.TeacherId == teacherId).ToListAsync();
            var courseIds = courses.Select(c => c.Id).ToList();

            var totalCourses = courses.Count;
            ViewData["RecentCourses"] = courses.OrderByDescending(c => c.Id).Take(4).ToList();
            ViewData["TotalCoursesCount"] = totalCourses;

            var totalStudents = await _context.Enrollments
                .Where(e => courseIds.Contains(e.CourseId))
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();
            
            var now = DateTime.UtcNow;

            var activeExams = await _context.Exams
                .Where(e => courseIds.Contains(e.CourseId) && e.IsPublished && now >= e.StartTime && now <= e.StartTime.AddMinutes(e.DurationMinutes))
                .CountAsync();

            var pendingGrading = await _context.Answers
                .Include(a => a.Submission)
                .Include(a => a.Question)
                    .ThenInclude(q => q.Exam)
                .Where(a => a.Question.Type == "ShortAnswer" 
                         && a.IsCorrect == null
                         && a.Submission.SubmittedAt != null
                         && courseIds.Contains(a.Question.Exam.CourseId))
                .CountAsync();

            var needsGradingList = await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Exam).ThenInclude(e => e.Course)
                .Include(s => s.Answers).ThenInclude(a => a.Question)
                .Where(s =>
                    s.SubmittedAt != null &&
                    s.IsTerminated == false &&
                    courseIds.Contains(s.Exam.CourseId) &&
                    s.Answers.Any(a =>
                        a.Question.Type == "ShortAnswer" &&
                        a.IsCorrect == null))
                .Select(s => new SmartExamAI.ViewModels.Teacher.NeedsGradingItem {
                    StudentName = s.Student.FullName,
                    ExamTitle = s.Exam.Title,
                    CourseTitle = s.Exam.Course.Title,
                    SubmittedAt = s.SubmittedAt ?? DateTime.UtcNow,
                    SubmissionId = s.Id
                })
                .OrderBy(s => s.SubmittedAt)
                .ToListAsync();

            var upcomingExams = await _context.Exams
                .Include(e => e.Course)
                .Where(e => courseIds.Contains(e.CourseId) 
                         && e.StartTime > now 
                         && e.StartTime <= now.AddHours(48))
                .OrderBy(e => e.StartTime)
                .Select(e => new SmartExamAI.ViewModels.Teacher.UpcomingExamItem
                {
                    ExamTitle = e.Title,
                    CourseTitle = e.Course.Title,
                    StartTime = e.StartTime
                })
                .ToListAsync();

            var latestSubmissions = await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Exam)
                .Where(s => courseIds.Contains(s.Exam.CourseId) && s.SubmittedAt != null)
                .OrderByDescending(s => s.SubmittedAt)
                .Take(5)
                .Select(s => new SmartExamAI.ViewModels.Teacher.ActivityItemViewModel
                {
                    StudentName = s.Student.FullName,
                    ExamTitle = s.Exam.Title,
                    Type = "Submission",
                    OccurredAt = s.SubmittedAt ?? DateTime.UtcNow
                })
                .ToListAsync();

            var latestViolations = await _context.Violations
                .Include(v => v.Submission).ThenInclude(s => s.Student)
                .Include(v => v.Submission).ThenInclude(s => s.Exam)
                .Where(v => courseIds.Contains(v.Submission.Exam.CourseId))
                .OrderByDescending(v => v.OccurredAt)
                .Take(5)
                .Select(v => new SmartExamAI.ViewModels.Teacher.ActivityItemViewModel
                {
                    StudentName = v.Submission.Student.FullName,
                    ExamTitle = v.Submission.Exam.Title,
                    Type = "Violation",
                    OccurredAt = v.OccurredAt
                })
                .ToListAsync();

            var recentActivity = latestSubmissions.Concat(latestViolations)
                .GroupBy(a => new { a.StudentName, a.OccurredAt.Date, a.OccurredAt.Hour })
                .Select(g => g.OrderByDescending(a => a.OccurredAt).First())
                .OrderByDescending(a => a.OccurredAt)
                .Take(8)
                .ToList();

            var model = new SmartExamAI.ViewModels.Teacher.TeacherDashboardViewModel
            {
                TotalCourses = totalCourses,
                TotalStudents = totalStudents,
                ActiveExams = activeExams,
                PendingGrading = pendingGrading,
                NeedsGradingList = needsGradingList,
                UpcomingExams = upcomingExams,
                RecentActivity = recentActivity
            };

            return View(model);
        }
    }
}
