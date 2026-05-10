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
    public class ExamsController : Controller
    {
        private const string McqType = "MCQ";
        private const string TrueFalseType = "TrueFalse";
        private const string ShortAnswerType = "ShortAnswer";

        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ExamsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create(int courseId)
        {
            var course = await GetOwnedCourseAsync(courseId, asNoTracking: true);
            if (course == null)
            {
                return NotFound();
            }

            ViewData["CourseTitle"] = course.Title;

            return View(new CreateExamViewModel
            {
                CourseId = course.Id,
                StartTime = DateTime.UtcNow.AddHours(1),
                DurationMinutes = 60,
                ViolationThreshold = 5
            });
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateExamViewModel model)
        {
            var course = await GetOwnedCourseAsync(model.CourseId, asNoTracking: true);
            if (course == null)
            {
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Course was not found." });
                }

                return NotFound();
            }

            if (model.StartTime == default)
            {
                ModelState.AddModelError(nameof(model.StartTime), "Start Time is required.");
            }

            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = GetModelStateErrorMessage() });
                }

                ViewData["CourseTitle"] = course.Title;
                return View(model);
            }

            var exam = new Exam
            {
                Title = model.Title.Trim(),
                CourseId = model.CourseId,
                StartTime = DateTime.SpecifyKind(model.StartTime, DateTimeKind.Local).ToUniversalTime(),
                DurationMinutes = model.DurationMinutes,
                ViolationThreshold = model.ViolationThreshold,
                QuestionRandomization = model.QuestionRandomization,
                IsPublished = false,
                ResultsPublished = false
            };

            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();

            if (IsAjaxRequest())
            {
                return Json(new { success = true, examId = exam.Id });
            }

            TempData["Success"] = "Exam created successfully.";
            return RedirectToAction(nameof(Details), new { id = exam.Id });
        }

        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var exam = await GetOwnedExamQuery(asNoTracking: true)
                .Include(e => e.Course)
                .Include(e => e.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null)
            {
                return NotFound();
            }

            var submissionCount = await _context.Submissions
                .AsNoTracking()
                .CountAsync(s => s.ExamId == exam.Id);

            var model = new ExamDetailsViewModel
            {
                ExamId = exam.Id,
                Title = exam.Title,
                CourseId = exam.CourseId,
                CourseTitle = exam.Course.Title,
                StartTime = exam.StartTime,
                DurationMinutes = exam.DurationMinutes,
                ViolationThreshold = exam.ViolationThreshold,
                QuestionRandomization = exam.QuestionRandomization,
                IsPublished = exam.IsPublished,
                Status = SmartExamAI.Helpers.ExamStatusHelper.GetStatus(exam.StartTime, exam.DurationMinutes),
                ResultsPublished = exam.ResultsPublished,
                SubmissionCount = submissionCount,
                Questions = exam.Questions
                    .OrderBy(q => q.OrderIndex)
                    .Select(ToQuestionViewModel)
                    .ToList()
            };

            var completedSubmissions = await _context.Submissions
                .AsNoTracking()
                .Where(s => s.ExamId == exam.Id && s.SubmittedAt != null && s.IsTerminated == false)
                .ToListAsync();

            model.TerminatedCount = await _context.Submissions
                .AsNoTracking()
                .CountAsync(s => s.ExamId == exam.Id && s.IsTerminated == true);

            model.TotalSubmissions = completedSubmissions.Count;
            if (model.TotalSubmissions > 0)
            {
                model.AverageScore = Math.Round(completedSubmissions.Average(s => (double)s.TotalScore), 1);
                
                double totalMarks = exam.Questions.Sum(q => q.Marks);
                double passingThreshold = totalMarks * 0.6;
                
                model.PassCount = completedSubmissions.Count(s => s.TotalScore >= passingThreshold);
                model.FailCount = completedSubmissions.Count(s => s.TotalScore < passingThreshold);
                model.PassRate = Math.Round(((double)model.PassCount.Value / model.TotalSubmissions) * 100, 1);
            }

            return View(model);
        }

        [HttpGet("Edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var exam = await GetOwnedExamQuery(asNoTracking: true)
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null)
            {
                return NotFound();
            }

            if (await HasSubmissionsAsync(exam.Id))
            {
                TempData["ErrorMessage"] = "Cannot edit an exam that already has submissions.";
                return RedirectToAction(nameof(Details), new { id = exam.Id });
            }

            ViewData["CourseTitle"] = exam.Course.Title;

            return View(new EditExamViewModel
            {
                Id = exam.Id,
                CourseId = exam.CourseId,
                Title = exam.Title,
                StartTime = exam.StartTime,
                DurationMinutes = exam.DurationMinutes,
                ViolationThreshold = exam.ViolationThreshold,
                QuestionRandomization = exam.QuestionRandomization
            });
        }

        [HttpPost("Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EditExamViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            var exam = await GetOwnedExamQuery()
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null)
            {
                return NotFound();
            }

            if (await HasSubmissionsAsync(exam.Id))
            {
                TempData["ErrorMessage"] = "Cannot edit an exam that already has submissions.";
                return RedirectToAction(nameof(Details), new { id = exam.Id });
            }

            if (model.StartTime == default)
            {
                ModelState.AddModelError(nameof(model.StartTime), "Start Time is required.");
            }

            if (!ModelState.IsValid)
            {
                ViewData["CourseTitle"] = exam.Course.Title;
                return View(model);
            }

            exam.Title = model.Title.Trim();
            exam.StartTime = DateTime.SpecifyKind(model.StartTime, DateTimeKind.Local).ToUniversalTime();
            exam.DurationMinutes = model.DurationMinutes;
            exam.ViolationThreshold = model.ViolationThreshold;
            exam.QuestionRandomization = model.QuestionRandomization;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Exam updated successfully.";
            return RedirectToAction(nameof(Details), new { id = exam.Id });
        }

        [HttpPost("Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var exam = await GetOwnedExamQuery()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null)
            {
                return NotFound();
            }

            var courseId = exam.CourseId;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var submissionIds = await _context.Submissions
                .Where(s => s.ExamId == exam.Id)
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
                .Where(q => q.ExamId == exam.Id)
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

            _context.Exams.Remove(exam);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] = "Exam deleted successfully.";
            return RedirectToAction("Details", "Courses", new { area = "Teacher", id = courseId });
        }

        [HttpPost("TogglePublish/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var exam = await GetOwnedExamQuery()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (exam == null)
            {
                return Json(new { success = false, message = "Exam was not found." });
            }

            exam.IsPublished = !exam.IsPublished;
            await _context.SaveChangesAsync();

            TempData["Success"] = exam.IsPublished ? "Exam is now live for students." : "Exam unpublished successfully.";
            return Json(new { success = true, isPublished = exam.IsPublished });
        }

        [HttpPost("AddQuestion")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(AddQuestionViewModel model)
        {
            var exam = await GetOwnedExamQuery()
                .FirstOrDefaultAsync(e => e.Id == model.ExamId);

            if (exam == null)
            {
                return Json(new { success = false, message = "Exam was not found." });
            }

            if (await HasSubmissionsAsync(exam.Id))
            {
                return Json(new { success = false, message = "This exam has submissions. Questions cannot be modified." });
            }

            var validationError = ValidateQuestionModel(model);
            if (!ModelState.IsValid || validationError != null)
            {
                return Json(new { success = false, message = validationError ?? GetModelStateErrorMessage() });
            }

            var nextOrder = await _context.Questions
                .Where(q => q.ExamId == model.ExamId)
                .Select(q => (int?)q.OrderIndex)
                .MaxAsync() ?? 0;

            var question = new Question
            {
                ExamId = model.ExamId,
                Text = model.Text.Trim(),
                Type = model.Type,
                Marks = model.Marks,
                OrderIndex = nextOrder + 1
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            var options = BuildQuestionOptions(question.Id, model);
            if (options.Count > 0)
            {
                _context.QuestionOptions.AddRange(options);
                await _context.SaveChangesAsync();
            }

            question.Options = options;

            return Json(new { success = true, question = ToQuestionViewModel(question) });
        }

        [HttpPost("DeleteQuestion/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _context.Questions
                .Include(q => q.Exam)
                    .ThenInclude(e => e.Course)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null || question.Exam.Course.TeacherId != _userManager.GetUserId(User))
            {
                return Json(new { success = false, message = "Question was not found." });
            }

            if (await HasSubmissionsAsync(question.ExamId))
            {
                return Json(new { success = false, message = "This exam has submissions. Questions cannot be modified." });
            }

            var options = await _context.QuestionOptions
                .Where(o => o.QuestionId == question.Id)
                .ToListAsync();

            _context.QuestionOptions.RemoveRange(options);
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            var remainingQuestions = await _context.Questions
                .Where(q => q.ExamId == question.ExamId)
                .OrderBy(q => q.OrderIndex)
                .ThenBy(q => q.Id)
                .ToListAsync();

            for (var i = 0; i < remainingQuestions.Count; i++)
            {
                remainingQuestions[i].OrderIndex = i + 1;
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost("ImportQuestions")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportQuestions(int examId, IFormFile? csvFile)
        {
            var exam = await GetOwnedExamQuery()
                .FirstOrDefaultAsync(e => e.Id == examId);

            if (exam == null)
            {
                return Json(new { success = false, message = "Exam was not found." });
            }

            if (await HasSubmissionsAsync(exam.Id))
            {
                return Json(new { success = false, message = "This exam has submissions. Questions cannot be modified." });
            }

            if (csvFile == null || csvFile.Length == 0)
            {
                return Json(new { success = false, message = "Please choose a CSV file." });
            }

            var parsedResult = await ParseQuestionCsvAsync(csvFile);
            if (!parsedResult.Succeeded)
            {
                return Json(new { success = false, message = parsedResult.ErrorMessage });
            }

            var importedQuestions = new List<QuestionViewModel>();
            var orderIndex = await _context.Questions
                .Where(q => q.ExamId == examId)
                .Select(q => (int?)q.OrderIndex)
                .MaxAsync() ?? 0;

            await using var transaction = await _context.Database.BeginTransactionAsync();

            foreach (var parsedQuestion in parsedResult.Questions)
            {
                orderIndex++;

                var question = new Question
                {
                    ExamId = examId,
                    Text = parsedQuestion.Text,
                    Type = parsedQuestion.Type,
                    Marks = parsedQuestion.Marks,
                    OrderIndex = orderIndex
                };

                _context.Questions.Add(question);
                await _context.SaveChangesAsync();

                var options = parsedQuestion.Options
                    .Select(option => new QuestionOption
                    {
                        QuestionId = question.Id,
                        Text = option.Text,
                        IsCorrect = option.IsCorrect
                    })
                    .ToList();

                if (options.Count > 0)
                {
                    _context.QuestionOptions.AddRange(options);
                    await _context.SaveChangesAsync();
                }

                question.Options = options;
                importedQuestions.Add(ToQuestionViewModel(question));
            }

            await transaction.CommitAsync();

            return Json(new { success = true, imported = importedQuestions.Count, questions = importedQuestions });
        }

        private async Task<Course?> GetOwnedCourseAsync(int courseId, bool asNoTracking = false)
        {
            var teacherId = _userManager.GetUserId(User);
            if (teacherId == null)
            {
                return null;
            }

            var query = _context.Courses.Where(c => c.Id == courseId && c.TeacherId == teacherId);
            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            return await query.FirstOrDefaultAsync();
        }

        private IQueryable<Exam> GetOwnedExamQuery(bool asNoTracking = false)
        {
            var teacherId = _userManager.GetUserId(User);
            var query = _context.Exams.Where(e => e.Course.TeacherId == teacherId);

            if (asNoTracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }

        private async Task<bool> HasSubmissionsAsync(int examId)
        {
            return await _context.Submissions.AnyAsync(s => s.ExamId == examId);
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }

        private string GetModelStateErrorMessage()
        {
            return ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
                ?? "Please correct the form and try again.";
        }

        private static string? ValidateQuestionModel(AddQuestionViewModel model)
        {
            if (model.Type is not McqType and not TrueFalseType and not ShortAnswerType)
            {
                return "Question Type must be MCQ, TrueFalse, or ShortAnswer.";
            }

            if (model.Type == McqType)
            {
                var optionSlots = model.Options
                    .Take(4)
                    .Select((text, index) => new { Text = text?.Trim() ?? string.Empty, Index = index })
                    .ToList();

                if (optionSlots.Count(slot => !string.IsNullOrWhiteSpace(slot.Text)) < 2)
                {
                    return "MCQ questions require at least two options.";
                }

                if (model.CorrectOptionIndex == null || optionSlots.All(slot => slot.Index != model.CorrectOptionIndex.Value || string.IsNullOrWhiteSpace(slot.Text)))
                {
                    return "Choose a correct MCQ option.";
                }
            }

            if (model.Type == TrueFalseType)
            {
                if (!string.Equals(model.CorrectAnswer, "True", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(model.CorrectAnswer, "False", StringComparison.OrdinalIgnoreCase))
                {
                    return "Choose True or False as the correct answer.";
                }
            }

            return null;
        }

        private static List<QuestionOption> BuildQuestionOptions(int questionId, AddQuestionViewModel model)
        {
            if (model.Type == ShortAnswerType)
            {
                return new List<QuestionOption>();
            }

            if (model.Type == TrueFalseType)
            {
                var correctAnswer = string.Equals(model.CorrectAnswer, "True", StringComparison.OrdinalIgnoreCase)
                    ? "True"
                    : "False";

                return new List<QuestionOption>
                {
                    new QuestionOption { QuestionId = questionId, Text = "True", IsCorrect = correctAnswer == "True" },
                    new QuestionOption { QuestionId = questionId, Text = "False", IsCorrect = correctAnswer == "False" }
                };
            }

            return model.Options
                .Take(4)
                .Select((text, index) => new { Text = text?.Trim() ?? string.Empty, Index = index })
                .Where(option => !string.IsNullOrWhiteSpace(option.Text))
                .Select(option => new QuestionOption
                {
                    QuestionId = questionId,
                    Text = option.Text,
                    IsCorrect = option.Index == model.CorrectOptionIndex
                })
                .ToList();
        }

        private static QuestionViewModel ToQuestionViewModel(Question question)
        {
            return new QuestionViewModel
            {
                Id = question.Id,
                ExamId = question.ExamId,
                Text = question.Text,
                Type = question.Type,
                Marks = question.Marks,
                OrderIndex = question.OrderIndex,
                Options = question.Options
                    .OrderBy(o => o.Id)
                    .Select(o => new QuestionOptionViewModel
                    {
                        Id = o.Id,
                        QuestionId = o.QuestionId,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect
                    })
                    .ToList()
            };
        }

        private static async Task<(bool Succeeded, string? ErrorMessage, List<ParsedQuestion> Questions)> ParseQuestionCsvAsync(IFormFile file)
        {
            var questions = new List<ParsedQuestion>();

            using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var rowNumber = 0;
            string? line;

            while ((line = await reader.ReadLineAsync()) != null)
            {
                rowNumber++;

                if (rowNumber == 1 || string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var columns = ParseCsvLine(line);
                if (columns.Count == 0)
                {
                    continue;
                }

                var type = columns[0].Trim();
                var questionText = columns.Count > 1 ? columns[1].Trim() : string.Empty;

                if (string.IsNullOrWhiteSpace(questionText))
                {
                    return (false, $"Row {rowNumber}: QuestionText is required.", questions);
                }

                if (type == McqType)
                {
                    if (columns.Count != 8)
                    {
                        return (false, $"Row {rowNumber}: MCQ rows must have 8 columns.", questions);
                    }

                    if (!int.TryParse(columns[2].Trim(), out var marks) || marks < 1)
                    {
                        return (false, $"Row {rowNumber}: Marks must be a positive number.", questions);
                    }

                    if (!int.TryParse(columns[7].Trim(), out var correctIndex) || correctIndex < 1 || correctIndex > 4)
                    {
                        return (false, $"Row {rowNumber}: CorrectIndex must be between 1 and 4.", questions);
                    }

                    var optionTexts = columns.Skip(3).Take(4).Select(c => c.Trim()).ToList();
                    if (optionTexts.Any(string.IsNullOrWhiteSpace))
                    {
                        return (false, $"Row {rowNumber}: MCQ options cannot be empty.", questions);
                    }

                    questions.Add(new ParsedQuestion
                    {
                        Type = McqType,
                        Text = questionText,
                        Marks = marks,
                        Options = optionTexts
                            .Select((text, index) => new ParsedOption { Text = text, IsCorrect = index == correctIndex - 1 })
                            .ToList()
                    });
                    continue;
                }

                if (type == TrueFalseType)
                {
                    if (columns.Count != 4)
                    {
                        return (false, $"Row {rowNumber}: TrueFalse rows must have 4 columns.", questions);
                    }

                    if (!int.TryParse(columns[2].Trim(), out var marks) || marks < 1)
                    {
                        return (false, $"Row {rowNumber}: Marks must be a positive number.", questions);
                    }

                    var correctAnswer = columns[3].Trim();
                    if (!string.Equals(correctAnswer, "True", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(correctAnswer, "False", StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, $"Row {rowNumber}: CorrectAnswer must be True or False.", questions);
                    }

                    questions.Add(new ParsedQuestion
                    {
                        Type = TrueFalseType,
                        Text = questionText,
                        Marks = marks,
                        Options = new List<ParsedOption>
                        {
                            new ParsedOption { Text = "True", IsCorrect = string.Equals(correctAnswer, "True", StringComparison.OrdinalIgnoreCase) },
                            new ParsedOption { Text = "False", IsCorrect = string.Equals(correctAnswer, "False", StringComparison.OrdinalIgnoreCase) }
                        }
                    });
                    continue;
                }

                if (type == ShortAnswerType)
                {
                    if (columns.Count != 3)
                    {
                        return (false, $"Row {rowNumber}: ShortAnswer rows must have 3 columns.", questions);
                    }

                    if (!int.TryParse(columns[2].Trim(), out var marks) || marks < 1)
                    {
                        return (false, $"Row {rowNumber}: Marks must be a positive number.", questions);
                    }

                    questions.Add(new ParsedQuestion
                    {
                        Type = ShortAnswerType,
                        Text = questionText,
                        Marks = marks
                    });
                    continue;
                }

                return (false, $"Row {rowNumber}: Question Type must be MCQ, TrueFalse, or ShortAnswer.", questions);
            }

            return (true, null, questions);
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

        private sealed class ParsedQuestion
        {
            public string Type { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
            public int Marks { get; set; }
            public List<ParsedOption> Options { get; set; } = new List<ParsedOption>();
        }

        private sealed class ParsedOption
        {
            public string Text { get; set; } = string.Empty;
            public bool IsCorrect { get; set; }
        }
    }
}
