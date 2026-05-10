namespace SmartExamAI.ViewModels.Teacher
{
    public class GradeSubmissionViewModel
    {
        public int SubmissionId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public int TotalScore { get; set; }
        public int MaxScore { get; set; }
        public bool IsTerminated { get; set; }
        public List<GradeAnswerViewModel> Answers { get; set; } = new List<GradeAnswerViewModel>();
    }
}
