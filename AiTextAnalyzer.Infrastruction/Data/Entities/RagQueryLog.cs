using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Infrastruction.Data.Entities
{
    public class RagQueryLog
    {
        public int Id { get; set; }

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public string Question { get; set; } = "";

        public string Answer { get; set; } = "";

        // "1,4,7"
        public string CitationsCsv { get; set; } = "";
    }
}
