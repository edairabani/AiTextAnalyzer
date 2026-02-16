using AiTextAnalyzer.Application.Rag;
using AiTextAnalyzer.Infrastruction.Data;
using AiTextAnalyzer.Infrastruction.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Infrastruction.Rag
{
    public class EfRagLogRepository : IRagLogRepository
    {
        private readonly VectorDbContext _db;
        public EfRagLogRepository(VectorDbContext db) => _db = db;

        public async Task SaveAsync(string question, string answer, IEnumerable<int> citationIds, CancellationToken ct)
        {
            _db.RagLogs.Add(new RagQueryLog
            {
                Question = question,
                Answer = answer,
                CitationsCsv = string.Join(",", citationIds)
            });

            await _db.SaveChangesAsync(ct);
        }
    }
}
