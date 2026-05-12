using SmartExamAI.Data;
using SmartExamAI.Models;

namespace SmartExamAI.Services
{
    public class NotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task NotifyAsync(string userId, string title, string message, string type, string? link = null)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Link = link
            });
            await _context.SaveChangesAsync();
        }

        public async Task NotifyManyAsync(IEnumerable<string> userIds, string title, string message, string type, string? link = null)
        {
            var notifications = userIds.Select(uid => new Notification
            {
                UserId = uid,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Link = link
            });

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
        }
    }
}
