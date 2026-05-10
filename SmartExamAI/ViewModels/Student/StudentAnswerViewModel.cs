namespace SmartExamAI.ViewModels.Student
{
    public class StudentAnswerViewModel
    {
        public int QuestionId { get; set; }
        public int? SelectedOptionId { get; set; }
        public string? TextAnswer { get; set; }
    }
}
