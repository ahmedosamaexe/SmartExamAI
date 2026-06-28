using SmartExamAI.Models;
using SmartExamAI.Repositories;
using SmartExamAI.ViewModels.Teacher;
using SmartExamAI.ViewModels.Student;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartExamAI.Services
{
    public class DashboardService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IExamRepository _examRepository;

        public DashboardService(ICourseRepository courseRepository, IExamRepository examRepository)
        {
            _courseRepository = courseRepository;
            _examRepository = examRepository;
        }

        public async Task<TeacherDashboardViewModel> GetTeacherDashboardDataAsync(string teacherId)
        {
            var courses = (await _courseRepository.GetByTeacherIdAsync(teacherId)).ToList();
            var courseIds = courses.Select(c => c.Id).ToList();

            int activeExams = 0;
            var allExams = new List<Exam>();
            foreach (var c in courses)
            {
                var exams = await _examRepository.GetByCourseIdAsync(c.Id);
                allExams.AddRange(exams);
                activeExams += exams.Count(e => e.IsPublished && e.IsActive());
            }

            int distinctStudents = await _examRepository.GetDistinctStudentsCountForCoursesAsync(courseIds);
            int pendingGrading = await _examRepository.GetPendingGradingAnswersCountAsync(courseIds);
            int totalSubmissions = await _examRepository.GetTotalSubmissionsCountForCoursesAsync(courseIds);

            var needsGradingSubmissions = await _examRepository.GetNeedsGradingSubmissionsAsync(courseIds);
            var needsGradingList = needsGradingSubmissions.Select(s => new NeedsGradingItem
            {
                SubmissionId = s.Id,
                StudentName = s.Student?.FullName ?? string.Empty,
                ExamTitle = s.Exam?.Title ?? string.Empty,
                CourseTitle = s.Exam?.Course?.Title ?? string.Empty,
                SubmittedAt = s.SubmittedAt ?? DateTime.UtcNow
            }).ToList();

            var now = DateTime.UtcNow;
            var upcomingList = allExams
                .Where(e => e.IsPublished && now <= e.EndTime)
                .OrderBy(e => e.StartTime)
                .Take(5)
                .Select(e => new UpcomingExamItem
                {
                    ExamId = e.Id,
                    ExamTitle = e.Title,
                    CourseTitle = e.Course?.Title ?? string.Empty,
                    StartTime = e.StartTime,
                    DurationMinutes = e.DurationMinutes,
                    IsActive = now >= e.StartTime && now <= e.EndTime
                }).ToList();

            var recentSubs = await _examRepository.GetRecentSubmissionsForCoursesAsync(courseIds, 5);
            var recentActivityList = recentSubs.Select(s => new ActivityItemViewModel
            {
                StudentName = s.Student?.FullName ?? string.Empty,
                ExamTitle = s.Exam?.Title ?? string.Empty,
                Type = "Submission",
                OccurredAt = s.SubmittedAt ?? DateTime.UtcNow
            }).ToList();

            var combinedActivity = recentActivityList
                .OrderByDescending(a => a.OccurredAt)
                .Take(6)
                .ToList();

            var recentCoursesList = courses.Take(4).Select(c =>
            {
                var courseExams = allExams.Where(e => e.CourseId == c.Id).ToList();
                return new TeacherCourseItemViewModel
                {
                    CourseId = c.Id,
                    Title = c.Title,
                    Tagline = c.Tagline ?? string.Empty,
                    ExamsCount = courseExams.Count,
                    ActiveExamsCount = courseExams.Count(e => e.IsPublished && e.IsActive())
                };
            }).ToList();

            return new TeacherDashboardViewModel
            {
                TotalCourses = courses.Count,
                ActiveExams = activeExams,
                TotalStudents = distinctStudents,
                TotalSubmissions = totalSubmissions,
                PendingGrading = pendingGrading,
                NeedsGradingList = needsGradingList,
                UpcomingExams = upcomingList,
                RecentActivity = combinedActivity,
                RecentCourses = recentCoursesList
            };
        }

        public async Task<StudentDashboardViewModel> GetStudentDashboardDataAsync(string studentId)
        {
            var enrollments = await _courseRepository.GetEnrollmentsByStudentIdAsync(studentId);
            var courseIds = enrollments.Select(e => e.CourseId).ToList();
            var publishedExams = (await _examRepository.GetPublishedByCourseIdsAsync(courseIds)).ToList();
            var submissions = (await _examRepository.GetSubmissionsByStudentIdAsync(studentId)).ToList();

            var now = DateTime.UtcNow;
            var takenExamIds = submissions.Where(s => s.SubmittedAt != null || s.IsTerminated).Select(s => s.ExamId).ToHashSet();

            var upcomingExams = publishedExams
                .Where(e => now <= e.EndTime && !takenExamIds.Contains(e.Id))
                .OrderBy(e => e.StartTime)
                .Select(e => new StudentUpcomingExamItem
                {
                    ExamId = e.Id,
                    ExamTitle = e.Title,
                    CourseTitle = e.Course?.Title ?? string.Empty,
                    StartTime = e.StartTime,
                    DurationMinutes = e.DurationMinutes,
                    IsActiveNow = now >= e.StartTime && now <= e.EndTime
                }).ToList();

            var completedList = new List<RecentResultItem>();
            foreach (var sub in submissions.Where(s => s.SubmittedAt != null || s.IsTerminated))
            {
                var exam = sub.Exam;
                if (exam == null) continue;

                int score = sub.Answers?.Sum(a => a.Score) ?? sub.TotalScore;
                int maxScore = exam.Questions?.Sum(q => q.Marks) ?? 0;
                bool hasPending = sub.Answers?.Any(a => a.Question?.Type == "ShortAnswer" && a.IsCorrect == null) ?? false;

                completedList.Add(new RecentResultItem
                {
                    SubmissionId = sub.Id,
                    ExamTitle = exam.Title,
                    CourseTitle = exam.Course?.Title ?? string.Empty,
                    TotalScore = score,
                    MaxScore = maxScore,
                    SubmittedAt = sub.SubmittedAt,
                    ResultsPublished = exam.ResultsPublished,
                    IsTerminated = sub.IsTerminated,
                    HasPendingGrading = hasPending
                });
            }

            var gradedResults = completedList.Where(c => c.MaxScore > 0 && c.ResultsPublished && !c.HasPendingGrading && !c.IsTerminated).ToList();
            double? avgScore = gradedResults.Any()
                ? gradedResults.Average(c => ((double)c.TotalScore / c.MaxScore) * 100)
                : null;

            return new StudentDashboardViewModel
            {
                EnrolledCourses = enrollments.Count(),
                ActiveExamsCount = upcomingExams.Count(x => x.IsActiveNow),
                CompletedExams = completedList.Count,
                AverageScore = avgScore != null ? Math.Round(avgScore.Value, 1) : null,
                UpcomingExams = upcomingExams,
                RecentResults = completedList.OrderByDescending(c => c.SubmissionId).Take(5).ToList()
            };
        }
    }
}
