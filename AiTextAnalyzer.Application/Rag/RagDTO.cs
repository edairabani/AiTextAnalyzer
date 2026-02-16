using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Application.Rag
{
    public record RagCitationDTO(int Id, string Snippet, double Distance);
    public record RagDTO(string Answer, RagCitationDTO[] Citations);
}
