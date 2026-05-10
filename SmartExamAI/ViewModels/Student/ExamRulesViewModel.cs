namespace SmartExamAI.ViewModels.Student
{
    public class ExamRulesViewModel
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public int ViolationThreshold { get; set; }
        public bool QuestionRandomization { get; set; }
        public string CourseName { get; set; } = string.Empty;
    }
}
