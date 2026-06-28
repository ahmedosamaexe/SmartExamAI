using System.ComponentModel.DataAnnotations;

namespace SmartExamAI.ViewModels.Teacher
{
    public class AddStudentManualViewModel
    {
        [Required]
        public int CourseId { get; set; }

        [Required(ErrorMessage = "Full Name is required.")]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email Address.")]
        [MaxLength(256)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;
    }
}
