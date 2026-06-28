namespace SmartExamAI.ViewModels.Student
{
    public class TakeExamViewModel
    {
        public int SubmissionId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime EndTimeUtc { get; set; }
        public int TotalSeconds { get; set; }
        public int ViolationThreshold { get; set; }
        public int WarningCount { get; set; }
        public List<ExamQuestionViewModel> Questions { get; set; } = new List<ExamQuestionViewModel>();
    }
}
