using AiTextAnalyzer.Application.AI;
using AiTextAnalyzer.Application.Vector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Application.Search
{
    public class SearchChunks
    {
        private readonly IEmbeddingProvider _embeddings;
        private readonly IVectorStore _store;

        public SearchChunks(IEmbeddingProvider embeddings, IVectorStore store)
        {
            _embeddings = embeddings;
            _store = store;
        }

        public async Task<IReadOnlyList<VectorSearchResult>> HandleAsync(string query, int topK, CancellationToken ct)
        {
            var emb = await _embeddings.CreateEmbeddingAsync(query, ct);
            return await _store.SearchAsync(emb, topK, ct);
        }
    }
}
