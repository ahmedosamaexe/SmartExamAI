using System.ComponentModel.DataAnnotations;

namespace SmartExamAI.ViewModels.Teacher
{
    public class SaveGradeViewModel
    {
        [Required]
        public int AnswerId { get; set; }

        [Required]
        [Range(0, 9999)]
        public int Score { get; set; }

        public string? TeacherFeedback { get; set; }
    }
}
