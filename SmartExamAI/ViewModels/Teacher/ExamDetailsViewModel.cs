namespace SmartExamAI.ViewModels.Teacher
{
    public class ExamDetailsViewModel
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int DurationMinutes { get; set; }
        public int ViolationThreshold { get; set; }
        public bool QuestionRandomization { get; set; }
        public bool IsPublished { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool ResultsPublished { get; set; }
        public int SubmissionCount { get; set; }
        public List<QuestionViewModel> Questions { get; set; } = new List<QuestionViewModel>();
        
        public double? AverageScore { get; set; }
        public int? PassCount { get; set; }
        public int? FailCount { get; set; }
        public int TotalSubmissions { get; set; }
        public double? PassRate { get; set; }
        public int TerminatedCount { get; set; }
    }
}
