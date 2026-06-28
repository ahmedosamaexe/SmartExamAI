namespace SmartExamAI.ViewModels.Teacher
{
    public class BulkGradeViewModel
    {
        public int SubmissionId { get; set; }
        public List<SaveGradeViewModel> Grades { get; set; } = new List<SaveGradeViewModel>();
    }
}
