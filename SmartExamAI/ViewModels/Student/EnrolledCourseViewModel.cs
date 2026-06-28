namespace SmartExamAI.ViewModels.Student
{
    public class EnrolledCourseViewModel
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public int ExamCount { get; set; }
        public double CompletedExamsPercent { get; set; }
        public bool HasActiveExam { get; set; }
    }
}
