namespace SmartExamAI.ViewModels.Teacher
{
    public class GradeExamViewModel
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public int TotalSubmissions { get; set; }
        public int GradedSubmissions { get; set; }
        public bool AllGraded { get; set; }
        public bool ResultsPublished { get; set; }
        public List<SubmissionRowViewModel> Submissions { get; set; } = new List<SubmissionRowViewModel>();
    }
}
