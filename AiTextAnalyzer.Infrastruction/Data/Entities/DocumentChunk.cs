using Pgvector;

namespace AiTextAnalyzer.Infrastruction.Data.Entities
{
    public class DocumentChunk
    {
        public int Id { get; set; }
        public string Content { get; set; } = "";
        public  Pgvector.Vector Embedding { get; set; } = default!;
    }
}
