using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SmartExamAI.Services
{
    public interface IAiService
    {
        bool IsEnabled { get; }
        Task<string?> GenerateQuestionsAsync(string topic, string questionType, int count, int marks);
        Task<AiGradingSuggestion?> SuggestGradeAsync(string questionText, string studentAnswer, int maxMarks);
    }

    public class AiGradingSuggestion
    {
        public int SuggestedScore { get; set; }
        public string Feedback { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }

    public class GeminiAiService : IAiService
    {
        private readonly string? _apiKey;
        private readonly HttpClient _httpClient;
        private const string GeminiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

        public GeminiAiService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _apiKey = configuration["AppSettings:AiApiKey"];
            _httpClient = httpClientFactory.CreateClient("GeminiAI");
        }

        public bool IsEnabled => !string.IsNullOrWhiteSpace(_apiKey);

        public async Task<string?> GenerateQuestionsAsync(string topic, string questionType, int count, int marks)
        {
            if (!IsEnabled) return null;

            var prompt = $@"Generate {count} {questionType} questions about ""{topic}"". Each question is worth {marks} marks.

Return ONLY valid JSON array. Each object must have:
- ""text"": the question text
- ""type"": ""{questionType}""
- ""marks"": {marks}
- ""options"": array of option objects (only for MCQ/TrueFalse), each with ""text"" and ""isCorrect"" (boolean)

For ShortAnswer type, set ""options"" to an empty array.
For TrueFalse type, options should be [{{""text"":""True"",""isCorrect"":true/false}},{{""text"":""False"",""isCorrect"":true/false}}].

Return ONLY the JSON array, no markdown, no explanation.";

            return await CallGeminiAsync(prompt);
        }

        public async Task<AiGradingSuggestion?> SuggestGradeAsync(string questionText, string studentAnswer, int maxMarks)
        {
            if (!IsEnabled) return null;

            var prompt = $@"You are grading a student's answer.

Question: {questionText}
Student Answer: {studentAnswer}
Maximum Marks: {maxMarks}

Evaluate the answer and return ONLY valid JSON:
{{
  ""suggestedScore"": <number between 0 and {maxMarks}>,
  ""feedback"": ""<brief feedback for the student>"",
  ""confidence"": <number between 0.0 and 1.0>
}}

Return ONLY the JSON, no markdown, no explanation.";

            var result = await CallGeminiAsync(prompt);
            if (result == null) return null;

            try
            {
                return JsonSerializer.Deserialize<AiGradingSuggestion>(result, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> CallGeminiAsync(string prompt)
        {
            if (!IsEnabled) return null;

            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                        new { parts = new[] { new { text = prompt } } }
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var url = $"{GeminiUrl}?key={_apiKey}";

                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode) return null;

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var text = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                // Clean markdown code fences if present
                if (text != null)
                {
                    text = text.Trim();
                    if (text.StartsWith("```json")) text = text[7..];
                    else if (text.StartsWith("```")) text = text[3..];
                    if (text.EndsWith("```")) text = text[..^3];
                    text = text.Trim();
                }

                return text;
            }
            catch
            {
                return null;
            }
        }
    }
}
