using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Application.Rag
{
    public interface IRagLogRepository
    {
        Task SaveAsync(string question, string answer, IEnumerable<int> citationIds, CancellationToken ct);
    }
}
