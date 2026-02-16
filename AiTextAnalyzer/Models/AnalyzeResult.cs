namespace AiTextAnalyzer.Models
{
    public record AnalyzeResult(string Sentiment, string Category, string Summary);
    public record AnalyzeRequest(string Text);

}
