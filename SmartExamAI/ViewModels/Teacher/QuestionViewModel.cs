namespace SmartExamAI.ViewModels.Teacher
{
    public class QuestionViewModel
    {
        public int Id { get; set; }
        public int ExamId { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Marks { get; set; }
        public int OrderIndex { get; set; }
        public List<QuestionOptionViewModel> Options { get; set; } = new List<QuestionOptionViewModel>();
    }
}
