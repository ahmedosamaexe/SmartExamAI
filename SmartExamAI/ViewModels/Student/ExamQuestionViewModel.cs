namespace SmartExamAI.ViewModels.Student
{
    public class ExamQuestionViewModel
    {
        public int QuestionId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Marks { get; set; }
        public int OrderIndex { get; set; }
        public List<ExamOptionViewModel> Options { get; set; } = new List<ExamOptionViewModel>();
    }
}
