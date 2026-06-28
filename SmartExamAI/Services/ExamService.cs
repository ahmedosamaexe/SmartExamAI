using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using SmartExamAI.Models;
using SmartExamAI.Repositories;
using SmartExamAI.ViewModels.Student;
using SmartExamAI.ViewModels.Teacher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartExamAI.Services
{
    public class ExamService
    {
        private readonly IExamRepository _examRepository;
        private readonly ICourseRepository _courseRepository;

        private const string McqType = "MCQ";
        private const string TrueFalseType = "TrueFalse";
        private const string ShortAnswerType = "ShortAnswer";

        public ExamService(IExamRepository examRepository, ICourseRepository courseRepository)
        {
            _examRepository = examRepository;
            _courseRepository = courseRepository;
        }

        // ── Teacher Exam Operations ──

        public async Task<Course?> GetOwnedCourseAsync(int courseId, string teacherId)
        {
            var course = await _courseRepository.GetByIdAsync(courseId);
            if (course != null && course.TeacherId == teacherId)
            {
                return course;
            }
            return null;
        }

        public async Task<CreateExamViewModel?> GetCreateExamViewModelAsync(int courseId, string teacherId)
        {
            var course = await GetOwnedCourseAsync(courseId, teacherId);
            if (course == null) return null;

            return new CreateExamViewModel
            {
                CourseId = course.Id,
                StartTime = DateTime.UtcNow.AddHours(1),
                DurationMinutes = 60,
                ViolationThreshold = 5
            };
        }

        public async Task<(bool Succeeded, string? ErrorMessage, Exam? Exam)> CreateExamFromModelAsync(CreateExamViewModel model, string teacherId)
        {
            var course = await GetOwnedCourseAsync(model.CourseId, teacherId);
            if (course == null)
            {
                return (false, "Course was not found.", null);
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

            await _examRepository.AddAsync(exam);
            await _examRepository.SaveChangesAsync();

            return (true, null, exam);
        }

        public async Task<ExamDetailsViewModel?> GetTeacherExamDetailsAsync(int examId, string teacherId)
        {
            var exam = await _examRepository.GetByIdWithDetailsAsync(examId);
            if (exam == null || exam.Course.TeacherId != teacherId)
            {
                return null;
            }

            var submissionCount = await _examRepository.GetSubmissionsCountByExamIdAsync(exam.Id);

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
                Status = exam.GetStatus(),
                ResultsPublished = exam.ResultsPublished,
                SubmissionCount = submissionCount,
                Questions = exam.Questions
                    .OrderBy(q => q.OrderIndex)
                    .Select(ToQuestionViewModel)
                    .ToList()
            };

            var completedSubmissions = (await _examRepository.GetCompletedSubmissionsForExamAsync(exam.Id))
                .Where(s => s.SubmittedAt != null && !s.IsTerminated)
                .ToList();

            model.TerminatedCount = await _examRepository.GetTerminatedSubmissionsCountByExamIdAsync(exam.Id);
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

            return model;
        }

        public async Task<(EditExamViewModel? Model, string? ErrorMessage, int CourseId)> GetEditExamViewModelAsync(int examId, string teacherId)
        {
            var exam = await _examRepository.GetByIdWithCourseAsync(examId);
            if (exam == null || exam.Course.TeacherId != teacherId)
            {
                return (null, "Exam not found.", 0);
            }

            int subCount = await _examRepository.GetSubmissionsCountByExamIdAsync(exam.Id);
            if (subCount > 0)
            {
                return (null, "Cannot edit an exam that already has submissions.", exam.CourseId);
            }

            var model = new EditExamViewModel
            {
                Id = exam.Id,
                CourseId = exam.CourseId,
                Title = exam.Title,
                StartTime = exam.StartTime,
                DurationMinutes = exam.DurationMinutes,
                ViolationThreshold = exam.ViolationThreshold,
                QuestionRandomization = exam.QuestionRandomization
            };

            return (model, null, exam.CourseId);
        }

        public async Task<(bool Succeeded, string? ErrorMessage, int CourseId)> UpdateExamFromModelAsync(int examId, EditExamViewModel model, string teacherId)
        {
            var exam = await _examRepository.GetByIdWithCourseAsync(examId);
            if (exam == null || exam.Course.TeacherId != teacherId)
            {
                return (false, "Exam not found.", 0);
            }

            int subCount = await _examRepository.GetSubmissionsCountByExamIdAsync(exam.Id);
            if (subCount > 0)
            {
                return (false, "Cannot edit an exam that already has submissions.", exam.CourseId);
            }

            exam.Title = model.Title.Trim();
            exam.StartTime = DateTime.SpecifyKind(model.StartTime, DateTimeKind.Local).ToUniversalTime();
            exam.DurationMinutes = model.DurationMinutes;
            exam.ViolationThreshold = model.ViolationThreshold;
            exam.QuestionRandomization = model.QuestionRandomization;

            _examRepository.Update(exam);
            await _examRepository.SaveChangesAsync();

            return (true, null, exam.CourseId);
        }

        public async Task<(bool Succeeded, int CourseId)> DeleteExamByIdAsync(int examId, string teacherId)
        {
            var exam = await _examRepository.GetByIdWithCourseAsync(examId);
            if (exam == null || exam.Course.TeacherId != teacherId)
            {
                return (false, 0);
            }

            int courseId = exam.CourseId;
            _examRepository.Delete(exam);
            await _examRepository.SaveChangesAsync();

            return (true, courseId);
        }

        public async Task<(bool Succeeded, string? Message, bool IsPublished)> TogglePublishByIdAsync(int examId, string teacherId)
        {
            var exam = await _examRepository.GetByIdWithCourseAsync(examId);
            if (exam == null || exam.Course.TeacherId != teacherId)
            {
                return (false, "Exam was not found.", false);
            }

            exam.IsPublished = !exam.IsPublished;
            _examRepository.Update(exam);
            await _examRepository.SaveChangesAsync();

            var msg = exam.IsPublished ? "Exam is now live for students." : "Exam unpublished successfully.";
            return (true, msg, exam.IsPublished);
        }

        // ── Teacher Question Operations ──

        public async Task<(bool Succeeded, string? Message, QuestionViewModel? Question)> AddQuestionToExamAsync(AddQuestionViewModel model, string teacherId)
        {
            var exam = await _examRepository.GetByIdWithCourseAsync(model.ExamId);
            if (exam == null || exam.Course.TeacherId != teacherId)
            {
                return (false, "Exam was not found.", null);
            }

            int subCount = await _examRepository.GetSubmissionsCountByExamIdAsync(exam.Id);
            if (subCount > 0)
            {
                return (false, "This exam has submissions. Questions cannot be modified.", null);
            }

            var validationError = ValidateQuestionModel(model);
            if (validationError != null)
            {
                return (false, validationError, null);
            }

            var nextOrder = await _examRepository.GetMaxQuestionOrderIndexAsync(model.ExamId);

            var question = new Question
            {
                ExamId = model.ExamId,
                Text = model.Text.Trim(),
                Type = model.Type,
                Marks = model.Marks,
                OrderIndex = nextOrder + 1
            };

            await _examRepository.AddQuestionAsync(question);
            await _examRepository.SaveChangesAsync();

            var options = BuildQuestionOptions(question.Id, model);
            if (options.Count > 0)
            {
                await _examRepository.AddQuestionOptionsAsync(options);
                await _examRepository.SaveChangesAsync();
            }

            question.Options = options;
            return (true, null, ToQuestionViewModel(question));
        }

        public async Task<(bool Succeeded, string? Message)> DeleteQuestionByIdAsync(int questionId, string teacherId)
        {
            var question = await _examRepository.GetQuestionByIdAsync(questionId);
            if (question == null) return (false, "Question was not found.");

            var exam = await _examRepository.GetByIdWithCourseAsync(question.ExamId);
            if (exam == null || exam.Course.TeacherId != teacherId)
            {
                return (false, "Question was not found.");
            }

            int subCount = await _examRepository.GetSubmissionsCountByExamIdAsync(exam.Id);
            if (subCount > 0)
            {
                return (false, "This exam has submissions. Questions cannot be modified.");
            }

            var options = await _examRepository.GetOptionsByQuestionIdAsync(question.Id);
            _examRepository.DeleteQuestionOptions(options);
            _examRepository.DeleteQuestion(question);
            await _examRepository.SaveChangesAsync();

            var remainingQuestions = (await _examRepository.GetQuestionsByExamIdAsync(exam.Id))
                .OrderBy(q => q.OrderIndex)
                .ThenBy(q => q.Id)
                .ToList();

            for (var i = 0; i < remainingQuestions.Count; i++)
            {
                remainingQuestions[i].OrderIndex = i + 1;
                _examRepository.UpdateQuestion(remainingQuestions[i]);
            }

            await _examRepository.SaveChangesAsync();
            return (true, null);
        }


        // ── Teacher Results & Grading Operations ──

        public async Task<GradeExamViewModel?> GetGradeExamViewModelAsync(int examId, string teacherId)
        {
            var exam = await _examRepository.GetByIdWithCourseAsync(examId);
            if (exam == null || exam.Course.TeacherId != teacherId) return null;

            var enrolledStudents = (await _courseRepository.GetEnrollmentsByCourseIdAsync(exam.CourseId))
                .Select(e => e.Student)
                .ToList();

            var submissions = await _examRepository.GetCompletedSubmissionsForExamAsync(examId);
            var maxScore = await _examRepository.GetExamTotalMarksAsync(examId);

            var submissionRows = enrolledStudents.Select(student =>
            {
                var s = submissions.FirstOrDefault(sub => sub.StudentId == student.Id);
                if (s != null)
                {
                    var isPending = s.Answers.Any(a => a.Question.Type == ShortAnswerType && a.IsCorrect == null);
                    return new SubmissionRowViewModel
                    {
                        SubmissionId = s.Id,
                        StudentName = s.Student.FullName,
                        StudentEmail = s.Student.Email ?? "",
                        SubmittedAt = s.SubmittedAt,
                        TotalScore = s.TotalScore,
                        MaxScore = maxScore,
                        WarningCount = s.WarningCount,
                        IsTerminated = s.IsTerminated,
                        GradingStatus = isPending ? "Pending" : "Done",
                        IsAbsent = false
                    };
                }
                else
                {
                    return new SubmissionRowViewModel
                    {
                        SubmissionId = 0,
                        StudentName = student.FullName,
                        StudentEmail = student.Email ?? "",
                        SubmittedAt = null,
                        TotalScore = 0,
                        MaxScore = maxScore,
                        WarningCount = 0,
                        IsTerminated = false,
                        GradingStatus = "Done",
                        IsAbsent = true
                    };
                }
            }).OrderBy(s => s.StudentName).ToList();

            var totalRealSubmissions = submissionRows.Count(s => !s.IsAbsent);
            var gradedRealSubmissions = submissionRows.Count(s => !s.IsAbsent && s.GradingStatus == "Done");

            var model = new GradeExamViewModel
            {
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                CourseId = exam.CourseId,
                CourseTitle = exam.Course.Title,
                TotalSubmissions = totalRealSubmissions,
                GradedSubmissions = gradedRealSubmissions,
                ResultsPublished = exam.ResultsPublished,
                Submissions = submissionRows
            };

            model.AllGraded = model.TotalSubmissions > 0 && model.GradedSubmissions == model.TotalSubmissions;
            return model;
        }

        public async Task<GradeSubmissionViewModel?> GetGradeSubmissionViewModelAsync(int submissionId, string teacherId)
        {
            var submission = await _examRepository.GetSubmissionForGradingAsync(submissionId);
            if (submission == null || submission.Exam.Course.TeacherId != teacherId) return null;

            var maxScore = await _examRepository.GetExamTotalMarksAsync(submission.ExamId);

            var answerViewModels = submission.Answers
                .OrderBy(a => a.Question.OrderIndex)
                .Select(a =>
                {
                    var selectedOption = a.SelectedOptionId.HasValue
                        ? a.Question.Options.FirstOrDefault(o => o.Id == a.SelectedOptionId)
                        : null;
                    var correctOption = a.Question.Options.FirstOrDefault(o => o.IsCorrect);

                    return new GradeAnswerViewModel
                    {
                        AnswerId = a.Id,
                        QuestionId = a.QuestionId,
                        QuestionText = a.Question.Text,
                        QuestionType = a.Question.Type,
                        Marks = a.Question.Marks,
                        OrderIndex = a.Question.OrderIndex,
                        SelectedOptionText = selectedOption?.Text,
                        CorrectOptionText = correctOption?.Text,
                        TextAnswer = a.TextAnswer,
                        IsCorrect = a.IsCorrect,
                        Score = a.Score,
                        TeacherFeedback = a.TeacherFeedback
                    };
                }).ToList();

            return new GradeSubmissionViewModel
            {
                SubmissionId = submission.Id,
                ExamId = submission.ExamId,
                ExamTitle = submission.Exam.Title,
                StudentName = submission.Student.FullName,
                StudentEmail = submission.Student.Email ?? "",
                SubmittedAt = submission.SubmittedAt ?? submission.StartedAt,
                TotalScore = submission.TotalScore,
                MaxScore = maxScore,
                IsTerminated = submission.IsTerminated,
                Answers = answerViewModels
            };
        }

        public async Task<(bool Succeeded, string? Message, int NewTotalScore, int AnswerId, int Score)> GradeAnswerAsync(SaveGradeViewModel model, string teacherId)
        {
            var answer = await _examRepository.GetAnswerForGradingAsync(model.AnswerId);
            if (answer == null || answer.Submission.Exam.Course.TeacherId != teacherId)
            {
                return (false, "Answer not found or unauthorized.", 0, 0, 0);
            }

            if (model.Score > answer.Question.Marks)
            {
                return (false, $"Score cannot exceed maximum marks ({answer.Question.Marks}).", 0, 0, 0);
            }

            answer.Score = model.Score;
            answer.TeacherFeedback = model.TeacherFeedback;
            answer.IsCorrect = model.Score > 0 ? true : (model.Score == 0 ? false : null);

            _examRepository.UpdateAnswer(answer);
            await _examRepository.SaveChangesAsync();

            var allAnswers = await _examRepository.GetAnswersBySubmissionIdAsync(answer.SubmissionId);
            var submission = answer.Submission;
            submission.TotalScore = allAnswers.Sum(a => a.Score);

            _examRepository.UpdateSubmission(submission);
            await _examRepository.SaveChangesAsync();

            return (true, null, submission.TotalScore, answer.Id, answer.Score);
        }

        public async Task<(bool Succeeded, string? Message, int CourseId)> PublishResultsAsync(int examId, string teacherId)
        {
            var exam = await _examRepository.GetByIdWithCourseAsync(examId);
            if (exam == null || exam.Course.TeacherId != teacherId) return (false, "Exam not found.", 0);

            var submissions = await _examRepository.GetCompletedSubmissionsForExamAsync(examId);
            bool allGraded = !submissions.Any(s => s.Answers.Any(a => a.Question.Type == ShortAnswerType && a.IsCorrect == null));

            if (!allGraded && submissions.Any())
            {
                return (false, "Cannot publish results — some submissions are not fully graded yet.", exam.CourseId);
            }

            foreach (var s in submissions)
            {
                s.TotalScore = s.Answers.Sum(a => a.Score);
                _examRepository.UpdateSubmission(s);
            }

            exam.ResultsPublished = true;
            _examRepository.Update(exam);
            await _examRepository.SaveChangesAsync();

            return (true, "Results are now visible to students.", exam.CourseId);
        }

        public async Task<(bool Succeeded, string? Message, int NewTotalScore)> BulkGradeSubmissionAsync(BulkGradeViewModel model, string teacherId)
        {
            var submission = await _examRepository.GetSubmissionForGradingAsync(model.SubmissionId);
            if (submission == null || submission.Exam.Course.TeacherId != teacherId)
            {
                return (false, "Submission not found or unauthorized.", 0);
            }

            foreach (var grade in model.Grades)
            {
                var answer = submission.Answers.FirstOrDefault(a => a.Id == grade.AnswerId);
                if (answer == null) continue;

                if (grade.Score > answer.Question.Marks)
                {
                    return (false, $"Score for Q{answer.Question.OrderIndex} cannot exceed {answer.Question.Marks}.", 0);
                }

                answer.Score = grade.Score;
                answer.TeacherFeedback = grade.TeacherFeedback;
                answer.IsCorrect = grade.Score > 0 ? true : false;
                _examRepository.UpdateAnswer(answer);
            }

            submission.TotalScore = submission.Answers.Sum(a => a.Score);
            _examRepository.UpdateSubmission(submission);
            await _examRepository.SaveChangesAsync();

            return (true, null, submission.TotalScore);
        }

        public async Task<(byte[]? FileBytes, string? FileName, string? ErrorMessage, int CourseId)> ExportResultsExcelAsync(int examId, string teacherId)
        {
            var exam = await _examRepository.GetByIdWithCourseAsync(examId);
            if (exam == null || exam.Course.TeacherId != teacherId) return (null, null, "Exam not found.", 0);

            if (!exam.ResultsPublished)
            {
                return (null, null, "Results must be published before exporting.", exam.CourseId);
            }

            var submissions = (await _examRepository.GetCompletedSubmissionsForExamAsync(examId))
                .OrderBy(s => s.Student.FullName)
                .ToList();

            var maxScore = await _examRepository.GetExamTotalMarksAsync(examId);

            ExcelPackage.License.SetNonCommercialPersonal("SmartExamAI");
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Results");

            var headers = new[] { "Student Name", "Email", "Started At", "Submitted At", "Total Score", "Max Score", "Percentage", "Warnings", "Status" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
            }

            using (var range = worksheet.Cells[1, 1, 1, headers.Length])
            {
                range.Style.Font.Bold = true;
                range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#2C2C2C"));
            }

            int row = 2;
            foreach (var s in submissions)
            {
                worksheet.Cells[row, 1].Value = s.Student.FullName;
                worksheet.Cells[row, 2].Value = s.Student.Email;
                worksheet.Cells[row, 3].Value = s.StartedAt.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cells[row, 4].Value = s.SubmittedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
                worksheet.Cells[row, 5].Value = s.TotalScore;
                worksheet.Cells[row, 6].Value = maxScore;

                var pct = maxScore > 0 ? (decimal)s.TotalScore / maxScore : 0;
                worksheet.Cells[row, 7].Value = pct;
                worksheet.Cells[row, 7].Style.Numberformat.Format = "0.0%";

                worksheet.Cells[row, 8].Value = s.WarningCount;
                worksheet.Cells[row, 9].Value = s.IsTerminated ? "Terminated" : "Submitted";

                if (row % 2 != 0)
                {
                    using var r = worksheet.Cells[row, 1, row, headers.Length];
                    r.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    r.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#F5F4F0"));
                }
                row++;
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

            var safeTitle = new string(exam.Title.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
            var fileName = $"{safeTitle}_Results.xlsx";

            return (package.GetAsByteArray(), fileName, null, exam.CourseId);
        }



        // ── Student Exam & Taking Operations ──

        public async Task<(string? ErrorMessage, int RedirectCourseId, ExamRulesViewModel? Model)> GetStudentExamRulesAsync(int examId, string studentId)
        {
            var exam = await _examRepository.GetByIdWithCourseAsync(examId);
            if (exam == null) return ("Exam not found.", 0, null);

            if (!exam.IsPublished) return ("This exam is not currently available.", 0, null);

            var isEnrolled = await _courseRepository.IsEnrolledAsync(studentId, exam.CourseId);
            if (!isEnrolled) return ("You are not enrolled in this course.", 0, null);

            if (!exam.IsActive())
            {
                return ("This exam is not currently active.", exam.CourseId, null);
            }

            var submission = await _examRepository.GetStudentSubmissionForExamAsync(exam.Id, studentId);
            if (submission != null)
            {
                if (submission.IsTerminated) return ("TERMINATED", submission.Id, null);
                if (submission.SubmittedAt != null) return ("RESULT", submission.Id, null);
            }

            var model = new ExamRulesViewModel
            {
                ExamId = exam.Id,
                Title = exam.Title,
                DurationMinutes = exam.DurationMinutes,
                ViolationThreshold = exam.ViolationThreshold,
                QuestionRandomization = exam.QuestionRandomization,
                CourseName = exam.Course.Title
            };

            return (null, 0, model);
        }

        public async Task<(string? RedirectAction, int? RedirectSubmissionId, string? ErrorMessage, TakeExamViewModel? Model)> GetStudentTakeExamAsync(int examId, string studentId)
        {
            var exam = await _examRepository.GetByIdWithDetailsAsync(examId);
            if (exam == null) return (null, null, "Exam not found.", null);

            if (!exam.IsPublished) return ("Index", null, "This exam is not currently available.", null);

            var isEnrolled = await _courseRepository.IsEnrolledAsync(studentId, exam.CourseId);
            if (!isEnrolled) return ("Index", null, "You are not enrolled in this course.", null);

            var endTime = exam.EndTime;
            if (!exam.IsActive())
            {
                return ("Index", null, "This exam is not currently active.", null);
            }

            var submission = await _examRepository.GetStudentSubmissionForExamAsync(exam.Id, studentId);
            if (submission != null)
            {
                if (submission.IsTerminated) return ("Terminated", submission.Id, null, null);
                if (submission.SubmittedAt != null && !submission.IsTerminated)
                {
                    return ("Index", null, "You have already submitted this exam.", null);
                }
            }
            else
            {
                submission = new Submission
                {
                    ExamId = exam.Id,
                    StudentId = studentId,
                    StartedAt = DateTime.UtcNow,
                    SubmittedAt = null,
                    IsTerminated = false,
                    TotalScore = 0,
                    WarningCount = 0
                };

                await _examRepository.AddSubmissionAsync(submission);
                await _examRepository.SaveChangesAsync();

                foreach (var q in exam.Questions)
                {
                    var ans = new Answer
                    {
                        SubmissionId = submission.Id,
                        QuestionId = q.Id,
                        SelectedOptionId = null,
                        TextAnswer = null,
                        IsCorrect = null,
                        Score = 0,
                        TeacherFeedback = null
                    };
                    await _examRepository.AddAnswerAsync(ans);
                }
                await _examRepository.SaveChangesAsync();

                submission = await _examRepository.GetStudentSubmissionForExamAsync(exam.Id, studentId);
            }

            if (submission == null) return (null, null, "Unable to start submission.", null);

            var questions = exam.Questions.OrderBy(q => q.OrderIndex).ThenBy(q => q.Id).ToList();
            if (exam.QuestionRandomization)
            {
                questions = ShuffleQuestions(questions, submission.Id);
            }

            var orderedQuestions = questions.Select((q, index) => new ExamQuestionViewModel
            {
                QuestionId = q.Id,
                Text = q.Text,
                Type = q.Type,
                Marks = q.Marks,
                OrderIndex = index + 1,
                Options = q.Options.OrderBy(o => o.Id).Select(o => new ExamOptionViewModel
                {
                    OptionId = o.Id,
                    Text = o.Text
                }).ToList()
            }).ToList();

            var remainingSeconds = Math.Max(0, (int)Math.Floor((endTime - DateTime.UtcNow).TotalSeconds));

            var model = new TakeExamViewModel
            {
                SubmissionId = submission.Id,
                ExamId = exam.Id,
                ExamTitle = exam.Title,
                CourseTitle = exam.Course.Title,
                EndTimeUtc = endTime,
                TotalSeconds = remainingSeconds,
                ViolationThreshold = exam.ViolationThreshold,
                WarningCount = submission.WarningCount,
                Questions = orderedQuestions
            };

            return (null, null, null, model);
        }

        public async Task<(bool Succeeded, string? Message, int SubmissionId)> SubmitStudentExamAsync(SubmitExamViewModel model, string studentId)
        {
            var submission = await _examRepository.GetSubmissionWithAnswersAsync(model.SubmissionId);
            if (submission == null || submission.StudentId != studentId)
            {
                return (false, "Submission not found.", 0);
            }

            if (submission.SubmittedAt != null || submission.IsTerminated)
            {
                return (false, "Already submitted.", 0);
            }

            var submittedAnswers = model.Answers
                .GroupBy(a => a.QuestionId)
                .ToDictionary(g => g.Key, g => g.Last());

            foreach (var answer in submission.Answers)
            {
                var question = submission.Exam.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                if (question == null) continue;

                submittedAnswers.TryGetValue(question.Id, out var submittedAnswer);

                if (question.Type == ShortAnswerType)
                {
                    answer.SelectedOptionId = null;
                    answer.TextAnswer = submittedAnswer?.TextAnswer?.Trim();
                    answer.IsCorrect = null;
                    answer.Score = 0;
                    _examRepository.UpdateAnswer(answer);
                    continue;
                }

                answer.TextAnswer = null;
                answer.SelectedOptionId = submittedAnswer?.SelectedOptionId;

                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                var isCorrect = correctOption != null && answer.SelectedOptionId == correctOption.Id;
                answer.IsCorrect = isCorrect;
                answer.Score = isCorrect ? question.Marks : 0;
                _examRepository.UpdateAnswer(answer);
            }

            submission.SubmittedAt = DateTime.UtcNow;
            submission.TotalScore = submission.Answers.Sum(a => a.Score);
            _examRepository.UpdateSubmission(submission);
            await _examRepository.SaveChangesAsync();

            return (true, null, submission.Id);
        }

        public async Task<(bool Succeeded, string? Message, int WarningCount, bool IsTerminated)> RecordStudentWarningAsync(int submissionId, string studentId)
        {
            var submission = await _examRepository.GetSubmissionByIdAsync(submissionId);
            if (submission == null || submission.StudentId != studentId)
            {
                return (false, "Submission not found or unauthorized.", 0, false);
            }

            if (submission.IsTerminated || submission.SubmittedAt != null)
            {
                return (false, "Exam already ended.", submission.WarningCount, submission.IsTerminated);
            }

            submission.WarningCount++;
            var threshold = submission.Exam?.ViolationThreshold ?? 3;
            if (threshold <= 0) threshold = 3;

            if (submission.WarningCount >= threshold)
            {
                submission.IsTerminated = true;
            }

            _examRepository.UpdateSubmission(submission);
            await _examRepository.SaveChangesAsync();

            return (true, null, submission.WarningCount, submission.IsTerminated);
        }

        public async Task<Submission?> GetStudentTerminatedSubmissionAsync(int submissionId, string studentId)
        {
            var submission = await _examRepository.GetSubmissionWithAnswersAsync(submissionId);
            if (submission == null || submission.StudentId != studentId) return null;
            return submission;
        }

        public async Task<ExamResultDetailViewModel?> GetStudentExamResultDetailAsync(int submissionId, string studentId)
        {
            var submission = await _examRepository.GetSubmissionWithAnswersAsync(submissionId);
            if (submission == null || submission.StudentId != studentId) return null;

            var maxScore = await _examRepository.GetExamTotalMarksAsync(submission.ExamId);
            var percentage = maxScore == 0 ? 0 : Math.Round((decimal)submission.TotalScore / maxScore * 100, 2);

            return new ExamResultDetailViewModel
            {
                SubmissionId = submission.Id,
                ExamTitle = submission.Exam.Title,
                CourseTitle = submission.Exam.Course.Title,
                SubmittedAt = submission.SubmittedAt ?? DateTime.UtcNow,
                IsTerminated = submission.IsTerminated,
                ResultsPublished = submission.Exam.ResultsPublished,
                TotalScore = submission.TotalScore,
                MaxScore = maxScore,
                Percentage = percentage,
                Answers = submission.Answers
                    .OrderBy(a => a.Question.OrderIndex)
                    .Select(a => new StudentAnswerResultViewModel
                    {
                        QuestionId = a.QuestionId,
                        QuestionText = a.Question.Text,
                        QuestionType = a.Question.Type,
                        Marks = a.Question.Marks,
                        OrderIndex = a.Question.OrderIndex,
                        SelectedOptionText = a.SelectedOption?.Text,
                        CorrectOptionText = submission.Exam.ResultsPublished ? a.Question.Options.FirstOrDefault(o => o.IsCorrect)?.Text : null,
                        TextAnswer = a.TextAnswer,
                        IsCorrect = a.IsCorrect,
                        Score = a.Score,
                        TeacherFeedback = a.TeacherFeedback,
                        IsPending = a.IsCorrect == null && a.Question.Type == ShortAnswerType,
                        ResultsPublished = submission.Exam.ResultsPublished
                    }).ToList()
            };
        }

        public async Task<(bool Succeeded, string? Message)> SaveStudentAnswerAsync(SaveAnswerViewModel model, string studentId)
        {
            var answer = await _examRepository.GetStudentAnswerAsync(model.SubmissionId, model.QuestionId, studentId);
            if (answer == null) return (false, "Answer not found.");

            if (answer.Submission.SubmittedAt != null || answer.Submission.IsTerminated)
            {
                return (false, "Exam already ended.");
            }

            answer.SelectedOptionId = model.SelectedOptionId;
            answer.TextAnswer = model.TextAnswer;
            _examRepository.UpdateAnswer(answer);
            await _examRepository.SaveChangesAsync();

            return (true, null);
        }

        // ── Private Helpers ──

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
            if (model.Type == ShortAnswerType) return new List<QuestionOption>();

            if (model.Type == TrueFalseType)
            {
                var correctAnswer = string.Equals(model.CorrectAnswer, "True", StringComparison.OrdinalIgnoreCase) ? "True" : "False";
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
                }).ToList();
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
                Options = question.Options.OrderBy(o => o.Id).Select(o => new QuestionOptionViewModel
                {
                    Id = o.Id,
                    QuestionId = o.QuestionId,
                    Text = o.Text,
                    IsCorrect = o.IsCorrect
                }).ToList()
            };
        }



        private static List<Question> ShuffleQuestions(List<Question> questions, int submissionId)
        {
            var random = new Random(submissionId);
            var shuffled = questions.ToList();

            for (var i = shuffled.Count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            return shuffled;
        }


    }
}
