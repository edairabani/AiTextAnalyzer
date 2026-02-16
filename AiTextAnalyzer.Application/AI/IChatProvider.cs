using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Application.AI
{
    public interface IChatProvider
    {
        Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct);
    }
}
