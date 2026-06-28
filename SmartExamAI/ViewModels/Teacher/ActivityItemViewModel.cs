using System;

namespace SmartExamAI.ViewModels.Teacher
{
    public class ActivityItemViewModel
    {
        public string StudentName { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "Submission" or "Violation"
        public DateTime OccurredAt { get; set; }
    }
}
