using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartExamAI.Models
{
    public class Answer
    {
        public int Id { get; set; }

        [Required]
        public int SubmissionId { get; set; }

        [Required]
        public int QuestionId { get; set; }

        public int? SelectedOptionId { get; set; }

        [MaxLength(4000)]
        public string? TextAnswer { get; set; }

        public bool? IsCorrect { get; set; }

        public int Score { get; set; }

        [MaxLength(2000)]
        public string? TeacherFeedback { get; set; }

        // Navigation properties
        [ForeignKey("SubmissionId")]
        public Submission Submission { get; set; } = null!;

        [ForeignKey("QuestionId")]
        public Question Question { get; set; } = null!;

        [ForeignKey("SelectedOptionId")]
        public QuestionOption? SelectedOption { get; set; }
    }
}
