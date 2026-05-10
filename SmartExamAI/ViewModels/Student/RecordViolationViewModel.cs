using System.ComponentModel.DataAnnotations;

namespace SmartExamAI.ViewModels.Student
{
    public class RecordViolationViewModel
    {
        [Required]
        public int SubmissionId { get; set; }

        [Required]
        [RegularExpression("^(FullscreenExit|TabSwitch|WindowBlur)$", ErrorMessage = "Invalid violation type.")]
        public string? Type { get; set; }
    }
}
