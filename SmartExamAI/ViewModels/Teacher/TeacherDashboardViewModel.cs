using System;
using System.Collections.Generic;

namespace SmartExamAI.ViewModels.Teacher
{
    public class TeacherDashboardViewModel
    {
        public int TotalCourses { get; set; }
        public int TotalStudents { get; set; }
        public int ActiveExams { get; set; }
        public int PendingGrading { get; set; }

        public List<NeedsGradingItem> NeedsGradingList { get; set; } = new();
        public List<UpcomingExamItem> UpcomingExams { get; set; } = new();
        public List<ActivityItemViewModel> RecentActivity { get; set; } = new();
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
        public string ExamTitle { get; set; } = string.Empty;
        public string CourseTitle { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
    }
}
