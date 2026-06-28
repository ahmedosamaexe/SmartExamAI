using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using SmartExamAI.Models;
using SmartExamAI.Repositories;
using SmartExamAI.ViewModels.Student;
using SmartExamAI.ViewModels.Teacher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SmartExamAI.Services
{
    public class CourseService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IExamRepository _examRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public CourseService(
            ICourseRepository courseRepository,
            IExamRepository examRepository,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _courseRepository = courseRepository;
            _examRepository = examRepository;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        // ── Teacher Course Operations ──

        public async Task<IEnumerable<Course>> GetTeacherCoursesAsync(string teacherId)
        {
            return await _courseRepository.GetByTeacherIdAsync(teacherId);
        }

        public async Task<Course?> GetOwnedCourseAsync(int courseId, string teacherId)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course != null && course.TeacherId == teacherId)
            {
                return course;
            }
            return null;
        }

        public async Task<CourseDetailsViewModel?> GetTeacherCourseDetailsAsync(int courseId, string teacherId)
        {
            var course = await GetOwnedCourseAsync(courseId, teacherId);
            if (course == null) return null;

            var enrollments = await _courseRepository.GetEnrollmentsByCourseIdAsync(courseId);
            var students = enrollments.Select(e => new StudentRowViewModel
            {
                Id = e.Student.Id,
                FullName = e.Student.FullName,
                Email = e.Student.Email ?? string.Empty,
                EnrolledAt = e.EnrolledAt
            }).ToList();

            var examsList = await _examRepository.GetByCourseIdAsync(courseId);
            var exams = new List<ExamRowViewModel>();

            foreach (var e in examsList)
            {
                var fullExam = await _examRepository.GetByIdWithQuestionsAsync(e.Id);
                var qCount = fullExam?.Questions.Count ?? 0;
                var submissions = await _examRepository.GetSubmissionsByExamIdAsync(e.Id);
                exams.Add(new ExamRowViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    StartTime = e.StartTime,
                    DurationMinutes = e.DurationMinutes,
                    IsPublished = e.IsPublished,
                    QuestionCount = qCount,
                    SubmissionCount = submissions.Count(),
                    ResultsPublished = e.ResultsPublished,
                    Status = e.GetStatus()
                });
            }

            return new CourseDetailsViewModel
            {
                CourseId = course.Id,
                Title = course.Title,
                Tagline = course.Tagline ?? string.Empty,
                Color = course.Color,
                Category = course.Category,
                EnrollCode = course.EnrollCode,
                Students = students,
                Exams = exams
            };
        }

        public async Task<Course> CreateCourseAsync(CreateCourseViewModel model, string teacherId)
        {
            var course = new Course
            {
                Title = model.Title.Trim(),
                Tagline = model.Tagline?.Trim() ?? string.Empty,
                Description = model.Description?.Trim() ?? string.Empty,
                Color = string.IsNullOrWhiteSpace(model.Color) ? "#C8D8C8" : model.Color.Trim(),
                Category = string.IsNullOrWhiteSpace(model.Category) ? "Other" : model.Category.Trim(),
                TeacherId = teacherId,
                EnrollCode = await GenerateUniqueEnrollCodeAsync()
            };

            await _courseRepository.AddAsync(course);
            await _courseRepository.SaveChangesAsync();

            return course;
        }

        public async Task UpdateCourseAsync(Course course, EditCourseViewModel model)
        {
            course.Title = model.Title.Trim();
            course.EnrollCode = model.EnrollCode.Trim();
            course.Tagline = model.Tagline?.Trim() ?? string.Empty;
            course.Description = model.Description?.Trim() ?? string.Empty;
            course.Color = string.IsNullOrWhiteSpace(model.Color) ? "#C8D8C8" : model.Color.Trim();
            course.Category = string.IsNullOrWhiteSpace(model.Category) ? "Other" : model.Category.Trim();

            _courseRepository.Update(course);
            await _courseRepository.SaveChangesAsync();
        }

        public async Task DeleteCourseAsync(Course course)
        {
            _courseRepository.Delete(course);
            await _courseRepository.SaveChangesAsync();
        }

        public async Task<(bool Succeeded, string? ErrorMessage)> EnrollOrEnsureUserAsync(string email, string fullName, int courseId)
        {
            email = email.Trim();
            fullName = fullName.Trim();

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    Role = "Student",
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user, _configuration["AppSettings:DefaultStudentPassword"] ?? "Pass1234");
                if (!createResult.Succeeded)
                {
                    return (false, string.Join(" ", createResult.Errors.Select(e => e.Description)));
                }

                if (!await _roleManager.RoleExistsAsync("Student"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Student"));
                }

                var roleResult = await _userManager.AddToRoleAsync(user, "Student");
                if (!roleResult.Succeeded)
                {
                    return (false, string.Join(" ", roleResult.Errors.Select(e => e.Description)));
                }
            }

            var isAlreadyEnrolled = await _courseRepository.IsEnrolledAsync(user.Id, courseId);

            if (!isAlreadyEnrolled)
            {
                await _courseRepository.AddEnrollmentAsync(new Enrollment
                {
                    CourseId = courseId,
                    StudentId = user.Id,
                    EnrolledAt = DateTime.UtcNow
                });
                await _courseRepository.SaveChangesAsync();
            }

            return (true, null);
        }

        public async Task RemoveStudentAsync(int courseId, string studentId)
        {
            var enrollment = await _courseRepository.GetEnrollmentAsync(studentId, courseId);
            if (enrollment != null)
            {
                _courseRepository.DeleteEnrollment(enrollment);
                await _courseRepository.SaveChangesAsync();
            }
        }

        // ── Student Course Operations ──

        public async Task<List<EnrolledCourseViewModel>> GetStudentEnrolledCoursesAsync(string studentId)
        {
            var enrollments = await _courseRepository.GetEnrollmentsByStudentIdAsync(studentId);
            var submissions = await _examRepository.GetSubmissionsByStudentIdAsync(studentId);
            var result = new List<EnrolledCourseViewModel>();

            foreach (var e in enrollments)
            {
                var exams = await _examRepository.GetByCourseIdAsync(e.CourseId);
                var publishedExams = exams.Where(ex => ex.IsPublished).ToList();
                int examCount = publishedExams.Count;
                int completedCount = submissions.Count(s => s.Exam.CourseId == e.CourseId && (s.SubmittedAt != null || s.IsTerminated));
                double percent = examCount > 0 ? ((double)completedCount / examCount) * 100 : 0;

                bool hasActive = publishedExams.Any(ex => ex.IsActive());

                result.Add(new EnrolledCourseViewModel
                {
                    CourseId = e.Course.Id,
                    Title = e.Course.Title,
                    Tagline = e.Course.Tagline ?? string.Empty,
                    Color = e.Course.Color,
                    Category = e.Course.Category,
                    TeacherName = e.Course.Teacher.FullName,
                    ExamCount = examCount,
                    CompletedExamsPercent = percent,
                    HasActiveExam = hasActive
                });
            }

            return result;
        }

        public async Task<StudentCourseDetailsViewModel?> GetStudentCourseDetailsAsync(int courseId, string studentId)
        {
            var enrollment = await _courseRepository.GetEnrollmentAsync(studentId, courseId);
            if (enrollment == null) return null;

            var course = await _courseRepository.GetByIdWithTeacherAsync(courseId);
            if (course == null) return null;

            var exams = await _examRepository.GetByCourseIdAsync(courseId);
            var publishedExamsList = exams.Where(e => e.IsPublished).OrderBy(e => e.StartTime).ToList();
            var submissions = await _examRepository.GetSubmissionsByStudentIdAsync(studentId);

            var publishedExams = publishedExamsList.Select(e =>
            {
                var submission = submissions.FirstOrDefault(s => s.ExamId == e.Id);
                return new StudentExamRowViewModel
                {
                    ExamId = e.Id,
                    Title = e.Title,
                    StartTime = e.StartTime,
                    DurationMinutes = e.DurationMinutes,
                    Status = e.GetStatus(),
                    SubmissionId = submission?.Id,
                    HasResult = submission != null && (e.ResultsPublished || submission.IsTerminated)
                };
            }).ToList();

            return new StudentCourseDetailsViewModel
            {
                CourseId = course.Id,
                Title = course.Title,
                Tagline = course.Tagline ?? string.Empty,
                Color = course.Color,
                Category = course.Category,
                TeacherName = course.Teacher?.FullName ?? string.Empty,
                PublishedExams = publishedExams
            };
        }

        public async Task<(bool Success, string Message)> EnrollStudentWithCodeAsync(string enrollCode, string studentId)
        {
            if (string.IsNullOrWhiteSpace(enrollCode))
            {
                return (false, "Please enter an enrollment code.");
            }

            var normalizedCode = enrollCode.Trim();
            var allCourses = await _courseRepository.GetAllAsync();
            var course = allCourses.FirstOrDefault(c => string.Equals(c.EnrollCode, normalizedCode, StringComparison.OrdinalIgnoreCase));

            if (course == null)
            {
                return (false, "Invalid enrollment code.");
            }

            var isAlreadyEnrolled = await _courseRepository.IsEnrolledAsync(studentId, course.Id);
            if (isAlreadyEnrolled)
            {
                return (false, "You are already enrolled in this course.");
            }

            await _courseRepository.AddEnrollmentAsync(new Enrollment
            {
                CourseId = course.Id,
                StudentId = studentId,
                EnrolledAt = DateTime.UtcNow
            });
            await _courseRepository.SaveChangesAsync();

            return (true, "You have joined the course successfully.");
        }

        // ── Helpers ──

        private async Task<string> GenerateUniqueEnrollCodeAsync()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            while (true)
            {
                var code = new string(Enumerable.Range(0, 8)
                    .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])
                    .ToArray());

                var existing = await _courseRepository.GetByEnrollCodeAsync(code);
                if (existing == null)
                {
                    return code;
                }
            }
        }
    }
}
