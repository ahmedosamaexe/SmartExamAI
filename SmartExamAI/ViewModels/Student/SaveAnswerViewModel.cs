namespace SmartExamAI.ViewModels.Student
{
    public class SaveAnswerViewModel
    {
        public int SubmissionId { get; set; }
        public int QuestionId { get; set; }
        public int? SelectedOptionId { get; set; }
        public string? TextAnswer { get; set; }
    }
}
