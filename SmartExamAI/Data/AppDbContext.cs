using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartExamAI.Models;

namespace SmartExamAI.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Course> Courses { get; set; } = null!;
        public DbSet<Enrollment> Enrollments { get; set; } = null!;
        public DbSet<Exam> Exams { get; set; } = null!;
        public DbSet<Question> Questions { get; set; } = null!;
        public DbSet<QuestionOption> QuestionOptions { get; set; } = null!;
        public DbSet<Submission> Submissions { get; set; } = null!;
        public DbSet<Answer> Answers { get; set; } = null!;
        public DbSet<Violation> Violations { get; set; } = null!;
        public DbSet<Notification> Notifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // 1. Answers
            builder.Entity<Answer>()
                .HasOne(a => a.Submission)
                .WithMany(s => s.Answers)
                .HasForeignKey(a => a.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Answer>()
                .HasOne(a => a.SelectedOption)
                .WithMany()
                .HasForeignKey(a => a.SelectedOptionId)
                .OnDelete(DeleteBehavior.NoAction);

            // 2. Violations
            builder.Entity<Violation>()
                .HasOne(v => v.Submission)
                .WithMany(s => s.Violations)
                .HasForeignKey(v => v.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // 3. Submissions
            builder.Entity<Submission>()
                .HasOne(s => s.Exam)
                .WithMany(e => e.Submissions)
                .HasForeignKey(s => s.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Submission>()
                .HasOne(s => s.Student)
                .WithMany()
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // 4. QuestionOptions
            builder.Entity<QuestionOption>()
                .HasOne(qo => qo.Question)
                .WithMany(q => q.Options)
                .HasForeignKey(qo => qo.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // 5. Questions
            builder.Entity<Question>()
                .HasOne(q => q.Exam)
                .WithMany(e => e.Questions)
                .HasForeignKey(q => q.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            // 6. Exams
            builder.Entity<Exam>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Exams)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // 7. Enrollments
            builder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Enrollment>()
                .HasOne(e => e.Student)
                .WithMany()
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // 8. Course -> Teacher
            builder.Entity<Course>()
                .HasOne(c => c.Teacher)
                .WithMany()
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // 9. Notifications
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
