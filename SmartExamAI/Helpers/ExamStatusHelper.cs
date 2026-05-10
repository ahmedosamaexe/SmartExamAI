using System;

namespace SmartExamAI.Helpers
{
    public static class ExamStatusHelper
    {
        public static string GetStatus(DateTime startTime, int durationMinutes)
        {
            var now = DateTime.UtcNow;
            var endTime = startTime.AddMinutes(durationMinutes);
            if (now < startTime) return "Upcoming";
            if (now <= endTime) return "Active";
            return "Ended";
        }

        public static bool IsActive(DateTime startTime, int durationMinutes)
            => GetStatus(startTime, durationMinutes) == "Active";

        public static bool IsEnded(DateTime startTime, int durationMinutes)
            => GetStatus(startTime, durationMinutes) == "Ended";

        public static DateTime GetEndTime(DateTime startTime, int durationMinutes)
            => startTime.AddMinutes(durationMinutes);
    }
}
