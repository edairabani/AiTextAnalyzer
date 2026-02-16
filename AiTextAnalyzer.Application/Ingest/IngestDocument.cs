using AiTextAnalyzer.Application.AI;
using AiTextAnalyzer.Application.Text;
using AiTextAnalyzer.Application.Vector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Application.Ingest
{
    public class IngestDocument
    {
        private readonly IEmbeddingProvider _embeddings;
        private readonly IVectorStore _store;

        public IngestDocument(IEmbeddingProvider embeddings, IVectorStore store)
        {
            _embeddings = embeddings;
            _store = store;
        }

        public async Task<int> HandleAsync(string text, CancellationToken ct)
        {
            var chunks = TextChunker.Chunk(text);
            foreach (var chunk in chunks)
            {
                var emb = await _embeddings.CreateEmbeddingAsync(chunk, ct);
                await _store.StoreChunkAsync(chunk, emb, ct);
            }
            return chunks.Count;
        }
    }
}
