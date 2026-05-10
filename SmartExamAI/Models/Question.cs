using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartExamAI.Models
{
    public class Question
    {
        public int Id { get; set; }

        [Required]
        public int ExamId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Text { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty;

        public int Marks { get; set; }

        public int OrderIndex { get; set; }

        // Navigation properties
        [ForeignKey("ExamId")]
        public Exam Exam { get; set; } = null!;

        public List<QuestionOption> Options { get; set; } = new List<QuestionOption>();
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}
