namespace SmartExamAI.ViewModels.Teacher
{
    public class ExamRowViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsPublished { get; set; }
        public int QuestionCount { get; set; }
        public int SubmissionCount { get; set; }
        public bool ResultsPublished { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
