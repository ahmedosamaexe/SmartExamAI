using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartExamAI.Data;
using SmartExamAI.Models;
using SmartExamAI.ViewModels.Teacher;

namespace SmartExamAI.Areas.Teacher.Controllers
{
    [Area("Teacher")]
    [Authorize(Roles = "Teacher")]
    [Route("Teacher/[controller]")]
    public class CoursesController : Controller
    {
        private const string DefaultStudentPassword = "Pass1234";
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public CoursesController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null)
            {
                return Challenge();
            }

            var courses = await _context.Courses
                .AsNoTracking()
                .Where(c => c.TeacherId == teacherId)
                .OrderBy(c => c.Title)
                .ToListAsync();

            return View(courses);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View(new CreateCourseViewModel());
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateCourseViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null)
            {
                return Challenge();
            }

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

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            if (model.CsvFile is { Length: > 0 })
            {
                await ProcessCsvEnrollmentAsync(model.CsvFile, course.Id);
            }

            TempData["Success"] = "Course created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null)
            {
                return Challenge();
            }

            var course = await _context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id && c.TeacherId == teacherId);

            if (course == null)
            {
                return NotFound();
            }

            var students = await _context.Enrollments
                .AsNoTracking()
                .Include(e => e.Student)
                .Where(e => e.CourseId == id)
                .OrderBy(e => e.Student.FullName)
                .Select(e => new StudentRowViewModel
                {
                    Id = e.Student.Id,
                    FullName = e.Student.FullName,
                    Email = e.Student.Email ?? string.Empty,
                    EnrolledAt = e.EnrolledAt
                })
                .ToListAsync();

            var exams = await _context.Exams
                .AsNoTracking()
                .Where(e => e.CourseId == id)
                .OrderByDescending(e => e.StartTime)
                .Select(e => new ExamRowViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    StartTime = e.StartTime,
                    DurationMinutes = e.DurationMinutes,
                    IsPublished = e.IsPublished,
                    QuestionCount = e.Questions.Count,
                    SubmissionCount = e.Submissions.Count,
                    ResultsPublished = e.ResultsPublished,
                    Status = SmartExamAI.Helpers.ExamStatusHelper.GetStatus(e.StartTime, e.DurationMinutes)
                })
                .ToListAsync();

            var model = new CourseDetailsViewModel
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

            return View(model);
        }

        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var course = await GetOwnedCourseAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            ViewData["EnrollCode"] = course.EnrollCode;

            var model = new EditCourseViewModel
            {
                Id = course.Id,
                Title = course.Title,
                EnrollCode = course.EnrollCode,
                Tagline = course.Tagline,
                Description = course.Description,
                Color = course.Color,
                Category = course.Category
            };

            return View(model);
        }

        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditCourseViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var course = await GetOwnedCourseAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewData["EnrollCode"] = course.EnrollCode;
                return View(model);
            }

            course.Title = model.Title.Trim();
            course.EnrollCode = model.EnrollCode.Trim();
            course.Tagline = model.Tagline?.Trim() ?? string.Empty;
            course.Description = model.Description?.Trim() ?? string.Empty;
            course.Color = string.IsNullOrWhiteSpace(model.Color) ? "#C8D8C8" : model.Color.Trim();
            course.Category = string.IsNullOrWhiteSpace(model.Category) ? "Other" : model.Category.Trim();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Course updated successfully.";
            return RedirectToAction(nameof(Details), new { id = course.Id });
        }

        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await GetOwnedCourseAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var examIds = await _context.Exams
                .Where(e => e.CourseId == course.Id)
                .Select(e => e.Id)
                .ToListAsync();

            foreach (var examId in examIds)
            {
                var submissionIds = await _context.Submissions
                    .Where(s => s.ExamId == examId)
                    .Select(s => s.Id)
                    .ToListAsync();

                if (submissionIds.Count > 0)
                {
                    var answers = await _context.Answers
                        .Where(a => submissionIds.Contains(a.SubmissionId))
                        .ToListAsync();
                    _context.Answers.RemoveRange(answers);
                    await _context.SaveChangesAsync();

                    var violations = await _context.Violations
                        .Where(v => submissionIds.Contains(v.SubmissionId))
                        .ToListAsync();
                    _context.Violations.RemoveRange(violations);
                    await _context.SaveChangesAsync();

                    var submissions = await _context.Submissions
                        .Where(s => submissionIds.Contains(s.Id))
                        .ToListAsync();
                    _context.Submissions.RemoveRange(submissions);
                    await _context.SaveChangesAsync();
                }

                var questionIds = await _context.Questions
                    .Where(q => q.ExamId == examId)
                    .Select(q => q.Id)
                    .ToListAsync();

                if (questionIds.Count > 0)
                {
                    var options = await _context.QuestionOptions
                        .Where(o => questionIds.Contains(o.QuestionId))
                        .ToListAsync();
                    _context.QuestionOptions.RemoveRange(options);
                    await _context.SaveChangesAsync();

                    var questions = await _context.Questions
                        .Where(q => questionIds.Contains(q.Id))
                        .ToListAsync();
                    _context.Questions.RemoveRange(questions);
                    await _context.SaveChangesAsync();
                }

                var exam = await _context.Exams.FirstOrDefaultAsync(e => e.Id == examId);
                if (exam != null)
                {
                    _context.Exams.Remove(exam);
                    await _context.SaveChangesAsync();
                }
            }

            var enrollments = await _context.Enrollments
                .Where(e => e.CourseId == course.Id)
                .ToListAsync();
            _context.Enrollments.RemoveRange(enrollments);
            await _context.SaveChangesAsync();

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            TempData["Success"] = "Course deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:int}/AddStudent")]
        public async Task<IActionResult> AddStudentManual(int id)
        {
            var course = await GetOwnedCourseAsync(id, asNoTracking: true);
            if (course == null)
            {
                return NotFound();
            }

            ViewData["CourseTitle"] = course.Title;
            return View("AddStudent", new AddStudentManualViewModel { CourseId = id });
        }

        [HttpPost("{id:int}/AddStudent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudentManual(int id, AddStudentManualViewModel model)
        {
            if (id != model.CourseId)
            {
                return BadRequest();
            }

            var course = await GetOwnedCourseAsync(id, asNoTracking: true);
            if (course == null)
            {
                return NotFound();
            }

            ViewData["CourseTitle"] = course.Title;

            if (!ModelState.IsValid)
            {
                return View("AddStudent", model);
            }

            var result = await EnrollOrEnsureUserAsync(model.Email, model.FullName, id);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Unable to add this student.");
                return View("AddStudent", model);
            }

            return RedirectToAction(nameof(Details), new { id = course.Id });
        }

        [HttpPost("{id:int}/RemoveStudent")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveStudent(int id, string studentId)
        {
            var course = await GetOwnedCourseAsync(id, asNoTracking: true);
            if (course == null)
            {
                return NotFound();
            }

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.CourseId == id && e.StudentId == studentId);

            if (enrollment != null)
            {
                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = course.Id });
        }

        private async Task<Course?> GetOwnedCourseAsync(int id, bool asNoTracking = false)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null)
            {
                return null;
            }

            var query = _context.Courses.Where(c => c.Id == id && c.TeacherId == teacherId);
            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync();
        }

        private async Task<string> GenerateUniqueEnrollCodeAsync()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            while (true)
            {
                var code = new string(Enumerable.Range(0, 8)
                    .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)])
                    .ToArray());

                var exists = await _context.Courses.AnyAsync(c => c.EnrollCode == code);
                if (!exists)
                {
                    return code;
                }
            }
        }

        private async Task ProcessCsvEnrollmentAsync(IFormFile file, int courseId)
        {
            using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var isFirstRow = true;
            string? line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (isFirstRow)
                {
                    isFirstRow = false;
                    continue;
                }

                var values = ParseCsvLine(line);
                if (values.Count != 2)
                {
                    continue;
                }

                var fullName = values[0].Trim();
                var email = values[1].Trim();
                if (!string.IsNullOrWhiteSpace(fullName) && !string.IsNullOrWhiteSpace(email))
                {
                    await EnrollOrEnsureUserAsync(email, fullName, courseId);
                }
            }
        }

        private async Task<(bool Succeeded, string? ErrorMessage)> EnrollOrEnsureUserAsync(string email, string fullName, int courseId)
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

                var createResult = await _userManager.CreateAsync(user, DefaultStudentPassword);
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

            var isAlreadyEnrolled = await _context.Enrollments
                .AnyAsync(e => e.CourseId == courseId && e.StudentId == user.Id);

            if (!isAlreadyEnrolled)
            {
                _context.Enrollments.Add(new Enrollment
                {
                    CourseId = courseId,
                    StudentId = user.Id,
                    EnrolledAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return (true, null);
        }

        private static List<string> ParseCsvLine(string line)
        {
            var values = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            values.Add(current.ToString());
            return values;
        }
    }
}
