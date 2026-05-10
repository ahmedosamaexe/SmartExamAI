using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace SmartExamAI.ViewModels.Teacher
{
    public class CreateCourseViewModel
    {
        [Required(ErrorMessage = "Title is required.")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Enroll code is required.")]
        [MaxLength(50)]
        public string EnrollCode { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Tagline { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Color is required.")]
        [MaxLength(20)]
        public string Color { get; set; } = "#C8D8C8";

        [Required(ErrorMessage = "Category is required.")]
        [MaxLength(100)]
        public string Category { get; set; } = "Other";

        public IFormFile? CsvFile { get; set; }
    }
}
