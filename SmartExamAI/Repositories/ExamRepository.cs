using Microsoft.EntityFrameworkCore;
using SmartExamAI.Data;
using SmartExamAI.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartExamAI.Repositories
{
    public class ExamRepository : IExamRepository
    {
        private readonly ApplicationDbContext _context;

        public ExamRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // ── Exam Operations ──
        public async Task<Exam?> GetByIdAsync(int id)
        {
            return await _context.Exams.FindAsync(id);
        }

        public async Task<Exam?> GetByIdWithCourseAsync(int id)
        {
            return await _context.Exams
                .Include(e => e.Course)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Exam?> GetByIdWithQuestionsAsync(int id)
        {
            return await _context.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<Exam?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Exams
                .Include(e => e.Course)
                .Include(e => e.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IEnumerable<Exam>> GetByCourseIdAsync(int courseId)
        {
            return await _context.Exams
                .Where(e => e.CourseId == courseId)
                .OrderByDescending(e => e.StartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Exam>> GetPublishedByCourseIdsAsync(IEnumerable<int> courseIds)
        {
            return await _context.Exams
                .Include(e => e.Course)
                .Where(e => courseIds.Contains(e.CourseId) && e.IsPublished)
                .OrderBy(e => e.StartTime)
                .ToListAsync();
        }

        public async Task AddAsync(Exam exam)
        {
            await _context.Exams.AddAsync(exam);
        }

        public void Update(Exam exam)
        {
            _context.Exams.Update(exam);
        }

        public void Delete(Exam exam)
        {
            _context.Exams.Remove(exam);
        }

        // ── Question Operations ──
        public async Task<Question?> GetQuestionByIdAsync(int id)
        {
            return await _context.Questions.FindAsync(id);
        }

        public async Task<Question?> GetQuestionWithOptionsAsync(int id)
        {
            return await _context.Questions
                .Include(q => q.Options)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<IEnumerable<Question>> GetQuestionsByExamIdAsync(int examId)
        {
            return await _context.Questions
                .Include(q => q.Options)
                .Where(q => q.ExamId == examId)
                .ToListAsync();
        }

        public async Task<int> GetMaxQuestionOrderIndexAsync(int examId)
        {
            return await _context.Questions
                .Where(q => q.ExamId == examId)
                .Select(q => (int?)q.OrderIndex)
                .MaxAsync() ?? 0;
        }

        public async Task<IEnumerable<QuestionOption>> GetOptionsByQuestionIdAsync(int questionId)
        {
            return await _context.QuestionOptions
                .Where(o => o.QuestionId == questionId)
                .ToListAsync();
        }

        public async Task AddQuestionAsync(Question question)
        {
            await _context.Questions.AddAsync(question);
        }

        public async Task AddQuestionOptionAsync(QuestionOption option)
        {
            await _context.QuestionOptions.AddAsync(option);
        }

        public async Task AddQuestionOptionsAsync(IEnumerable<QuestionOption> options)
        {
            await _context.QuestionOptions.AddRangeAsync(options);
        }

        public void UpdateQuestion(Question question)
        {
            _context.Questions.Update(question);
        }

        public void DeleteQuestion(Question question)
        {
            _context.Questions.Remove(question);
        }

        public void DeleteQuestionOption(QuestionOption option)
        {
            _context.QuestionOptions.Remove(option);
        }

        public void DeleteQuestionOptions(IEnumerable<QuestionOption> options)
        {
            _context.QuestionOptions.RemoveRange(options);
        }

        // ── Submission & Answer Operations ──
        public async Task<Submission?> GetSubmissionByIdAsync(int id)
        {
            return await _context.Submissions.Include(s => s.Exam).FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Submission?> GetSubmissionWithAnswersAsync(int id)
        {
            return await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Course)
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Questions)
                        .ThenInclude(q => q.Options)
                .Include(s => s.Answers)
                    .ThenInclude(a => a.Question)
                        .ThenInclude(q => q.Options)
                .Include(s => s.Answers)
                    .ThenInclude(a => a.SelectedOption)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<IEnumerable<Submission>> GetSubmissionsByExamIdAsync(int examId)
        {
            return await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Answers)
                .Where(s => s.ExamId == examId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Submission>> GetSubmissionsByStudentIdAsync(string studentId)
        {
            return await _context.Submissions
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Course)
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Questions)
                .Include(s => s.Answers)
                    .ThenInclude(a => a.Question)
                .Where(s => s.StudentId == studentId)
                .ToListAsync();
        }

        public async Task<Submission?> GetStudentSubmissionForExamAsync(int examId, string studentId)
        {
            return await _context.Submissions
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Course)
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Questions)
                        .ThenInclude(q => q.Options)
                .Include(s => s.Answers)
                .FirstOrDefaultAsync(s => s.ExamId == examId && s.StudentId == studentId);
        }

        public async Task<Answer?> GetAnswerByIdAsync(int answerId)
        {
            return await _context.Answers
                .Include(a => a.Question)
                .Include(a => a.Submission)
                .FirstOrDefaultAsync(a => a.Id == answerId);
        }

        public async Task<IEnumerable<Submission>> GetCompletedSubmissionsForExamAsync(int examId)
        {
            return await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Answers)
                    .ThenInclude(a => a.Question)
                .Where(s => s.ExamId == examId && (s.SubmittedAt != null || s.IsTerminated))
                .ToListAsync();
        }

        public async Task<Submission?> GetSubmissionForGradingAsync(int submissionId)
        {
            return await _context.Submissions
                .Include(s => s.Exam)
                    .ThenInclude(e => e.Course)
                .Include(s => s.Student)
                .Include(s => s.Answers)
                    .ThenInclude(a => a.Question)
                        .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(s => s.Id == submissionId);
        }


        public async Task<int> GetSubmissionsCountByExamIdAsync(int examId)
        {
            return await _context.Submissions.AsNoTracking().CountAsync(s => s.ExamId == examId);
        }

        public async Task<int> GetTerminatedSubmissionsCountByExamIdAsync(int examId)
        {
            return await _context.Submissions.AsNoTracking().CountAsync(s => s.ExamId == examId && s.IsTerminated == true);
        }

        public async Task<int> GetExamTotalMarksAsync(int examId)
        {
            return await _context.Questions.Where(q => q.ExamId == examId).SumAsync(q => (int?)q.Marks) ?? 0;
        }

        public async Task<Answer?> GetAnswerForGradingAsync(int answerId)
        {
            return await _context.Answers
                .Include(a => a.Submission)
                    .ThenInclude(s => s.Exam)
                        .ThenInclude(e => e.Course)
                .Include(a => a.Question)
                .FirstOrDefaultAsync(a => a.Id == answerId);
        }

        public async Task<IEnumerable<Answer>> GetAnswersBySubmissionIdAsync(int submissionId)
        {
            return await _context.Answers.Where(a => a.SubmissionId == submissionId).ToListAsync();
        }

        public async Task<Answer?> GetStudentAnswerAsync(int submissionId, int questionId, string studentId)
        {
            return await _context.Answers
                .Include(a => a.Submission)
                .FirstOrDefaultAsync(a =>
                    a.SubmissionId == submissionId &&
                    a.QuestionId == questionId &&
                    a.Submission.StudentId == studentId);
        }

        public async Task AddSubmissionAsync(Submission submission)
        {
            await _context.Submissions.AddAsync(submission);
        }

        public async Task AddAnswerAsync(Answer answer)
        {
            await _context.Answers.AddAsync(answer);
        }


        public void UpdateSubmission(Submission submission)
        {
            _context.Submissions.Update(submission);
        }

        public void UpdateAnswer(Answer answer)
        {
            _context.Answers.Update(answer);
        }

        // ── Dashboard/Grading Queries ──
        public async Task<int> GetPendingGradingAnswersCountAsync(IEnumerable<int> courseIds)
        {
            return await _context.Answers
                .Include(a => a.Submission)
                .Include(a => a.Question)
                    .ThenInclude(q => q.Exam)
                .Where(a => a.Question.Type == "ShortAnswer" 
                         && a.IsCorrect == null
                         && a.Submission.SubmittedAt != null
                         && courseIds.Contains(a.Question.Exam.CourseId))
                .CountAsync();
        }

        public async Task<IEnumerable<Submission>> GetNeedsGradingSubmissionsAsync(IEnumerable<int> courseIds)
        {
            return await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Exam).ThenInclude(e => e.Course)
                .Include(s => s.Answers).ThenInclude(a => a.Question)
                .Where(s =>
                    s.SubmittedAt != null &&
                    s.IsTerminated == false &&
                    courseIds.Contains(s.Exam.CourseId) &&
                    s.Answers.Any(a =>
                        a.Question.Type == "ShortAnswer" &&
                        a.IsCorrect == null))
                .OrderBy(s => s.SubmittedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Submission>> GetRecentSubmissionsForCoursesAsync(IEnumerable<int> courseIds, int take)
        {
            return await _context.Submissions
                .Include(s => s.Student)
                .Include(s => s.Exam)
                .Where(s => courseIds.Contains(s.Exam.CourseId) && s.SubmittedAt != null)
                .OrderByDescending(s => s.SubmittedAt)
                .Take(take)
                .ToListAsync();
        }


        public async Task<int> GetDistinctStudentsCountForCoursesAsync(IEnumerable<int> courseIds)
        {
            return await _context.Enrollments
                .Where(e => courseIds.Contains(e.CourseId))
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();
        }

        public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
