using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Application.Vector
{
    public record VectorSearchResult(int Id, string Content, double Distance);
}
