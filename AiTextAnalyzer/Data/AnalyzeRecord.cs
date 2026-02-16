namespace AiTextAnalyzer.Data
{
    public class AnalyzeRecord
    {
        public int Id { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public string InputText { get; set; } = "";
        public string Sentiment { get; set; } = "";
        public string Category { get; set; } = "";
        public string Summary { get; set; } = "";
    }
}
