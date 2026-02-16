using Pgvector;

namespace AiTextAnalyzer.Data
{
    public class DocumentChunk
    {  
        public int Id { get; set; }
        public string Content { get; set; } = "";
        public Vector Embedding { get; set; } = new Vector(new float[1536]);

    }

   
}
