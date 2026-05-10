using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartExamAI.Models
{
    public class Exam
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public int CourseId { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public int DurationMinutes { get; set; }

        public int ViolationThreshold { get; set; }

        public bool IsPublished { get; set; }

        public bool ResultsPublished { get; set; }

        public bool QuestionRandomization { get; set; }

        // Computed property — NOT mapped to database
        [NotMapped]
        public DateTime EndTime => StartTime.AddMinutes(DurationMinutes);

        // Navigation properties
        [ForeignKey("CourseId")]
        public Course Course { get; set; } = null!;

        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
    }
}
