using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartExamAI.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        public bool IsRead { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Optional link to navigate to when notification is clicked</summary>
        [MaxLength(500)]
        public string? Link { get; set; }

        // Navigation
        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; } = null!;
    }
}
