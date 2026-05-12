using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartExamAI.Data;
using SmartExamAI.Models;

namespace SmartExamAI.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class NotificationsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return View(notifications);
        }

        [HttpGet("Recent")]
        public async Task<IActionResult> Recent()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Json(new { items = Array.Empty<object>() });

            var items = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(8)
                .Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.Type,
                    n.IsRead,
                    n.Link,
                    TimeAgo = GetTimeAgo(n.CreatedAt)
                })
                .ToListAsync();

            var unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return Json(new { items, unreadCount });
        }

        [HttpPost("MarkRead/{id:int}")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            var notif = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notif != null)
            {
                notif.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        [HttpPost("MarkAllRead")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Unauthorized();

            await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));

            return Json(new { success = true });
        }

        private static string GetTimeAgo(DateTime createdAt)
        {
            var diff = DateTime.UtcNow - createdAt;
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            return createdAt.ToString("MMM dd");
        }
    }
}
