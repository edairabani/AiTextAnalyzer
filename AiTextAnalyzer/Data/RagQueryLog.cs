namespace AiTextAnalyzer.Data
{
    public class RagQueryLog
    {
        public int Id { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public string Question { get; set; } = "";
        public string Answer { get; set; } = "";

        // Speichern als CSV: "1,7,12"
        public string CitationsCsv { get; set; } = "";
    }
}
