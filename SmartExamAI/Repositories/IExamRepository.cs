using SmartExamAI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartExamAI.Repositories
{
    public interface IExamRepository
    {
        // Exam Operations
        Task<Exam?> GetByIdAsync(int id);
        Task<Exam?> GetByIdWithCourseAsync(int id);
        Task<Exam?> GetByIdWithQuestionsAsync(int id);
        Task<Exam?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<Exam>> GetByCourseIdAsync(int courseId);
        Task<IEnumerable<Exam>> GetPublishedByCourseIdsAsync(IEnumerable<int> courseIds);
        Task AddAsync(Exam exam);
        void Update(Exam exam);
        void Delete(Exam exam);

        // Question Operations
        Task<Question?> GetQuestionByIdAsync(int id);
        Task<Question?> GetQuestionWithOptionsAsync(int id);
        Task<IEnumerable<Question>> GetQuestionsByExamIdAsync(int examId);
        Task<int> GetMaxQuestionOrderIndexAsync(int examId);
        Task<IEnumerable<QuestionOption>> GetOptionsByQuestionIdAsync(int questionId);
        Task AddQuestionAsync(Question question);
        Task AddQuestionOptionAsync(QuestionOption option);
        Task AddQuestionOptionsAsync(IEnumerable<QuestionOption> options);
        void UpdateQuestion(Question question);
        void DeleteQuestion(Question question);
        void DeleteQuestionOption(QuestionOption option);
        void DeleteQuestionOptions(IEnumerable<QuestionOption> options);

        // Submission & Answer Operations
        Task<Submission?> GetSubmissionByIdAsync(int id);
        Task<Submission?> GetSubmissionWithAnswersAsync(int id);
        Task<IEnumerable<Submission>> GetSubmissionsByExamIdAsync(int examId);
        Task<IEnumerable<Submission>> GetSubmissionsByStudentIdAsync(string studentId);
        Task<Submission?> GetStudentSubmissionForExamAsync(int examId, string studentId);
        Task<IEnumerable<Submission>> GetCompletedSubmissionsForExamAsync(int examId);
        Task<Submission?> GetSubmissionForGradingAsync(int submissionId);
        Task<int> GetSubmissionsCountByExamIdAsync(int examId);
        Task<int> GetTerminatedSubmissionsCountByExamIdAsync(int examId);
        Task<int> GetExamTotalMarksAsync(int examId);
        Task<Answer?> GetAnswerByIdAsync(int answerId);
        Task<Answer?> GetAnswerForGradingAsync(int answerId);
        Task<Answer?> GetStudentAnswerAsync(int submissionId, int questionId, string studentId);
        Task<IEnumerable<Answer>> GetAnswersBySubmissionIdAsync(int submissionId);
        Task AddSubmissionAsync(Submission submission);
        Task AddAnswerAsync(Answer answer);
        void UpdateSubmission(Submission submission);
        void UpdateAnswer(Answer answer);

        // Dashboard/Grading Queries
        Task<int> GetPendingGradingAnswersCountAsync(IEnumerable<int> courseIds);
        Task<IEnumerable<Submission>> GetNeedsGradingSubmissionsAsync(IEnumerable<int> courseIds);
        Task<IEnumerable<Submission>> GetRecentSubmissionsForCoursesAsync(IEnumerable<int> courseIds, int take);
        Task<int> GetDistinctStudentsCountForCoursesAsync(IEnumerable<int> courseIds);
        Task<int> GetTotalSubmissionsCountForCoursesAsync(IEnumerable<int> courseIds);

        Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync();
        Task<int> SaveChangesAsync();
    }
}
