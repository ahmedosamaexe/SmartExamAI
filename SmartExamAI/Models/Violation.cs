using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartExamAI.Models
{
    public class Violation
    {
        public int Id { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Type { get; set; } = string.Empty;

        public DateTime OccurredAt { get; set; }

        // Navigation properties
        [ForeignKey("SubmissionId")]
        public Submission Submission { get; set; } = null!;
    }
}
