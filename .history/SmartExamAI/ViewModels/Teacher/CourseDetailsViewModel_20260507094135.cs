namespace SmartExamAI.ViewModels.Teacher
{
    public class CourseDetailsViewModel
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string EnrollCode { get; set; } = string.Empty;

        public List<StudentRowViewModel> Students { get; set; } = new List<StudentRowViewModel>();
        public List<ExamRowViewModel> Exams { get; set; } = new List<ExamRowViewModel>();
    }
}
