using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Application.Rag
{
    public interface IRagService
    {
        Task<RagDTO> AskAsync(string question, int topK, double threshold, CancellationToken ct);
    }
}
