using System.ComponentModel.DataAnnotations;

namespace SmartExamAI.ViewModels.Teacher
{
    public class CreateExamViewModel
    {
        [Required]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start Time is required.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "Duration is required.")]
        [Range(1, 480, ErrorMessage = "Duration must be between 1 and 480 minutes.")]
        public int DurationMinutes { get; set; }

        [Required(ErrorMessage = "Violation Threshold is required.")]
        [Range(1, 100, ErrorMessage = "Violation Threshold must be at least 1.")]
        public int ViolationThreshold { get; set; } = 5;

        public bool QuestionRandomization { get; set; }
    }
}
