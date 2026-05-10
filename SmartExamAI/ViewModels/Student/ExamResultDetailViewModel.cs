namespace SmartExamAI.ViewModels.Student
{
    public class ExamResultDetailViewModel
    {
        public int SubmissionId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public bool IsTerminated { get; set; }
        public bool ResultsPublished { get; set; }
        public int TotalScore { get; set; }
        public int MaxScore { get; set; }
        public decimal Percentage { get; set; }
        public List<StudentAnswerResultViewModel> Answers { get; set; } = new List<StudentAnswerResultViewModel>();
    }
}
