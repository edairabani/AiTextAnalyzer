using AiTextAnalyzer.Application.AI;
using AiTextAnalyzer.Application.Vector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Application.Ingest
{
    public class StoreChunk
    {
        private readonly IEmbeddingProvider _embeddings;
        private readonly IVectorStore _store;

        public StoreChunk(IEmbeddingProvider embeddings, IVectorStore store)
        {
            _embeddings = embeddings;
            _store = store;
        }

        public async Task HandleAsync(string text, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text is required.", nameof(text));

            var emb = await _embeddings.CreateEmbeddingAsync(text, ct);
            await _store.StoreChunkAsync(text, emb, ct);
        }
    }
}
