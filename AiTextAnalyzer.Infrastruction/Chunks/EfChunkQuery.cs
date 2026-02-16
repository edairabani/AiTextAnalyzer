using AiTextAnalyzer.Application.Chunks;
using AiTextAnalyzer.Infrastruction.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Infrastruction.Chunks
{
    public class EfChunkQuery : IChunkQuery
    {
        private readonly VectorDbContext _db;

        public EfChunkQuery(VectorDbContext db)
        {
            _db = db;
        }

        public async Task<ChunkDto?> GetByIdAsync(int id, CancellationToken ct)
        {
            return await _db.Chunks
                .Where(x => x.Id == id)
                .Select(x => new ChunkDto(x.Id, x.Content))
                .FirstOrDefaultAsync(ct);
        }
    }
}
