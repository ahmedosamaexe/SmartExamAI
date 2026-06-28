using System.ComponentModel.DataAnnotations;

namespace SmartExamAI.ViewModels.Teacher
{
    public class AddQuestionViewModel
    {
        [Required]
        public int ExamId { get; set; }

        [Required(ErrorMessage = "Question Text is required.")]
        [MaxLength(2000)]
        public string Text { get; set; } = string.Empty;

        [Required(ErrorMessage = "Question Type is required.")]
        [RegularExpression("^(MCQ|TrueFalse|ShortAnswer)$", ErrorMessage = "Question Type must be MCQ, TrueFalse, or ShortAnswer.")]
        public string Type { get; set; } = "MCQ";

        [Required(ErrorMessage = "Marks are required.")]
        [Range(1, 1000, ErrorMessage = "Marks must be at least 1.")]
        public int Marks { get; set; } = 1;

        public List<string> Options { get; set; } = new List<string>();
        public int? CorrectOptionIndex { get; set; }
        public string? CorrectAnswer { get; set; }
    }
}
