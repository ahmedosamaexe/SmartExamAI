using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartExamAI.Models
{
    public class Submission
    {
        public int Id { get; set; }

        [Required]
        public int ExamId { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;

        public DateTime StartedAt { get; set; }

        public DateTime? SubmittedAt { get; set; }

        public bool IsTerminated { get; set; }

        public int TotalScore { get; set; }

        public int WarningCount { get; set; }

        // Navigation properties
        [ForeignKey("ExamId")]
        public Exam Exam { get; set; } = null!;

        [ForeignKey("StudentId")]
        public ApplicationUser Student { get; set; } = null!;

        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}
