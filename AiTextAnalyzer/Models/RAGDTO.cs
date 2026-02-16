namespace AiTextAnalyzer.Models
{
    public record RagAskRequest(string Question);
    public record RagCitation(int Id, string Snippet, double Distance);
    public record RagAskResponse(string Answer, RagCitation[] Citations);
}
