using System;
using System.Collections.Generic;

namespace SmartExamAI.ViewModels.Student
{
    public class StudentDashboardViewModel
    {
        public int EnrolledCourses { get; set; }
        public int ActiveExamsCount { get; set; }
        public int CompletedExams { get; set; }
        public double? AverageScore { get; set; }

        public List<StudentUpcomingExamItem> UpcomingExams { get; set; } = new();
        public List<RecentResultItem> RecentResults { get; set; } = new();
    }

    public class StudentUpcomingExamItem
    {
        public string ExamTitle { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int DurationMinutes { get; set; }
        public int ExamId { get; set; }
        public bool IsActiveNow { get; set; }
    }

    public class RecentResultItem
    {
        public string ExamTitle { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public int MaxScore { get; set; }
        public int SubmissionId { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public bool ResultsPublished { get; set; }
        public bool IsTerminated { get; set; }
        public bool HasPendingGrading { get; set; }
    }
}
