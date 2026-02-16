using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Application.Stream
{
    public record AnalyzeResult(string Sentiment, string Category, string Summary);
    public record AnalyzeRequest(string Text);
}
