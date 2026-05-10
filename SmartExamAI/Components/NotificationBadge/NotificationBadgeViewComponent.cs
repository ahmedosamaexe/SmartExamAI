using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartExamAI.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartExamAI.Components.NotificationBadge
{
    public class NotificationBadgeViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public NotificationBadgeViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!UserClaimsPrincipal.IsInRole("Teacher"))
            {
                return Content("");
            }

            var teacherId = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(teacherId))
            {
                return Content("");
            }

            var count = await _context.Submissions
                .Where(s => s.SubmittedAt != null 
                         && !s.IsTerminated 
                         && !s.Exam.ResultsPublished
                         && s.Exam.Course.TeacherId == teacherId)
                .Where(s => s.Answers.Any(a => a.Question.Type == "ShortAnswer" && a.IsCorrect == null))
                .Select(s => s.Id)
                .Distinct()
                .CountAsync();

            return View(count);
        }
    }
}
