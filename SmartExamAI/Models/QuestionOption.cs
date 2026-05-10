using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartExamAI.Models
{
    public class QuestionOption
    {
        public int Id { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required]
        [MaxLength(1000)]
        public string Text { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }

        // Navigation properties
        [ForeignKey("QuestionId")]
        public Question Question { get; set; } = null!;
    }
}
