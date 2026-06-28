namespace SmartExamAI.ViewModels.Student
{
    public class SubmitExamViewModel
    {
        public int SubmissionId { get; set; }
        public List<StudentAnswerViewModel> Answers { get; set; } = new List<StudentAnswerViewModel>();
    }
}
