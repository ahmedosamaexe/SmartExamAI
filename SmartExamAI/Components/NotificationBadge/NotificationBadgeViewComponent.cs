using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartExamAI.Data;
using System.Security.Claims;

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
            var userId = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Content("");
            }

            var unreadCount = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            return View(unreadCount);
        }
    }
}
