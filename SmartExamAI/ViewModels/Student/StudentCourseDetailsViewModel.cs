namespace SmartExamAI.ViewModels.Student
{
    public class StudentCourseDetailsViewModel
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;

        public List<StudentExamRowViewModel> PublishedExams { get; set; } = new List<StudentExamRowViewModel>();
    }
}
