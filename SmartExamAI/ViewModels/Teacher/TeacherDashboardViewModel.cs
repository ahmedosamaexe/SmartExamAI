using System;
using System.Collections.Generic;

namespace SmartExamAI.ViewModels.Teacher
{
    public class TeacherDashboardViewModel
    {
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
        public int ActiveExams { get; set; }
        public int TotalSubmissions { get; set; }
        public int PendingGrading { get; set; }

        public List<NeedsGradingItem> NeedsGradingList { get; set; } = new();
        public List<UpcomingExamItem> UpcomingExams { get; set; } = new();
        public List<ActivityItemViewModel> RecentActivity { get; set; } = new();
        public List<TeacherCourseItemViewModel> RecentCourses { get; set; } = new();
    }

    public class NeedsGradingItem
    {
        public string StudentName { get; set; } = string.Empty;
        public string ExamTitle { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public int SubmissionId { get; set; }
    }

    public class UpcomingExamItem
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsActive { get; set; }
    }

    public class TeacherCourseItemViewModel
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Tagline { get; set; } = string.Empty;
        public int ExamsCount { get; set; }
        public int ActiveExamsCount { get; set; }
    }
}
