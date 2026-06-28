using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartExamAI.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(20)]
        public string EnrollCode { get; set; } = string.Empty;

        [Required]
        public string TeacherId { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Color { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Tagline { get; set; }

        // Navigation properties
        [ForeignKey("TeacherId")]
        public ApplicationUser Teacher { get; set; } = null!;

        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }
}
