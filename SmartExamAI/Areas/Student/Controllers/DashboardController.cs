using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartExamAI.Data;
using SmartExamAI.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartExamAI.Areas.Student.Controllers
{
    [Area("Student")]
    [Authorize(Roles = "Student")]
    [Route("Student/[controller]")]
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
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == user.Id)
                .Select(e => e.CourseId)
                .ToListAsync();

            var now = DateTime.UtcNow;

            var completedSubmissions = await _context.Submissions
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Questions)
                .Where(s => s.StudentId == user.Id && s.SubmittedAt != null)
                .ToListAsync();

            var completedExamsCount = completedSubmissions.Count;
            
            double? averageScore = null;
            if (completedExamsCount > 0)
            {
                var validSubmissions = completedSubmissions.Where(s => s.Exam.Questions.Sum(q => q.Marks) > 0).ToList();
                if (validSubmissions.Any())
                {
                    averageScore = validSubmissions.Average(s => (double)s.TotalScore / s.Exam.Questions.Sum(q => q.Marks) * 100);
                }
            }

            var upcomingExams = await _context.Exams
                .Include(e => e.Course)
                .Where(e => enrolledCourseIds.Contains(e.CourseId) 
                         && e.IsPublished
                         && e.StartTime <= now.AddHours(48)
                         && e.StartTime.AddMinutes(e.DurationMinutes) > now
                         && !_context.Submissions.Any(s => s.ExamId == e.Id && s.StudentId == user.Id && (s.SubmittedAt != null || s.IsTerminated)))
                .OrderBy(e => e.StartTime)
                .Select(e => new SmartExamAI.ViewModels.Student.StudentUpcomingExamItem
                {
                    ExamId = e.Id,
                    ExamTitle = e.Title,
                    CourseTitle = e.Course.Title,
                    StartTime = e.StartTime
                })
                .ToListAsync();

            var recentResults = await _context.Submissions
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Course)
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Questions)
                .Where(s => s.StudentId == user.Id && s.SubmittedAt != null)
                .OrderByDescending(s => s.SubmittedAt)
                .Take(5)
                .Select(s => new SmartExamAI.ViewModels.Student.RecentResultItem
                {
                    SubmissionId = s.Id,
                    ExamTitle = s.Exam.Title,
                    CourseTitle = s.Exam.Course.Title,
                    TotalScore = s.TotalScore,
                    MaxScore = s.Exam.Questions.Sum(q => q.Marks)
                })
                .ToListAsync();

            var model = new SmartExamAI.ViewModels.Student.StudentDashboardViewModel
            {
                EnrolledCourses = enrolledCourseIds.Count,
                CompletedExams = completedExamsCount,
                AverageScore = averageScore,
                UpcomingExams = upcomingExams,
                RecentResults = recentResults
            };

            return View(model);
        }
    }
}
