using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Application.Vector
{
    public interface IVectorStore
    {
        Task StoreChunkAsync(string content, float[] embedding, CancellationToken ct);
        Task<IReadOnlyList<VectorSearchResult>> SearchAsync(float[] embedding, int topK, CancellationToken ct);
    }
}
