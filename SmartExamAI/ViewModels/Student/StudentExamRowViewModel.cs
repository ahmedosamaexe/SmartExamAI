namespace SmartExamAI.ViewModels.Student
{
    public class StudentExamRowViewModel
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int DurationMinutes { get; set; }
        public string Status { get; set; } = string.Empty; // Upcoming, Active, Ended
        public int? SubmissionId { get; set; }
        public bool HasResult { get; set; }
    }
}
