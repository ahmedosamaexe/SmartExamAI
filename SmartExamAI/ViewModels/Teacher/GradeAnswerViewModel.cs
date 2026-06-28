namespace SmartExamAI.ViewModels.Teacher
{
    public class GradeAnswerViewModel
    {
        public int AnswerId { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty;
        public int Marks { get; set; }
        public int OrderIndex { get; set; }
        public string? SelectedOptionText { get; set; }
        public string? CorrectOptionText { get; set; }
        public string? TextAnswer { get; set; }
        public bool? IsCorrect { get; set; }
        public int Score { get; set; }
        public string? TeacherFeedback { get; set; }
    }
}
