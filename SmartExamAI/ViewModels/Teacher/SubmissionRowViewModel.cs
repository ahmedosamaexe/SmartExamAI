namespace SmartExamAI.ViewModels.Teacher
{
    public class SubmissionRowViewModel
    {
        public int SubmissionId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public DateTime? SubmittedAt { get; set; }
        public int TotalScore { get; set; }
        public int MaxScore { get; set; }
        public int ViolationCount { get; set; }
        public bool IsTerminated { get; set; }
        public string GradingStatus { get; set; } = string.Empty; // "Pending" or "Done"
        public bool IsAbsent { get; set; }
    }
}
