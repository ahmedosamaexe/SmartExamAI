namespace SmartExamAI.ViewModels.Teacher
{
    public class ViolationStudentViewModel
    {
        public int SubmissionId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int ViolationCount { get; set; }
        public bool IsTerminated { get; set; }
        public List<ViolationDetailViewModel> Violations { get; set; } = new List<ViolationDetailViewModel>();
    }
}
