using SmartExamAI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartExamAI.Repositories
{
    public interface ICourseRepository
    {
        Task<IEnumerable<Course>> GetAllAsync();
        Task<IEnumerable<Course>> GetByTeacherIdAsync(string teacherId);
        Task<Course?> GetByIdAsync(int id);
        Task<Course?> GetByIdWithTeacherAsync(int id);
        Task<Course?> GetByEnrollCodeAsync(string enrollCode);
        Task AddAsync(Course course);
        void Update(Course course);
        void Delete(Course course);

        // Enrollment Operations
        Task<IEnumerable<Enrollment>> GetEnrollmentsByCourseIdAsync(int courseId);
        Task<IEnumerable<Enrollment>> GetEnrollmentsByStudentIdAsync(string studentId);
        Task<bool> IsEnrolledAsync(string studentId, int courseId);
        Task<Enrollment?> GetEnrollmentAsync(string studentId, int courseId);
        Task AddEnrollmentAsync(Enrollment enrollment);
        void DeleteEnrollment(Enrollment enrollment);

        Task<int> SaveChangesAsync();
    }
}
