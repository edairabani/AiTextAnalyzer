using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Application.AI
{
    public interface IEmbeddingProvider
    {
        Task<float[]> CreateEmbeddingAsync(string input, CancellationToken ct);
    }
}
