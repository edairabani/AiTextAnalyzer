using AiTextAnalyzer.Application.Vector;
using AiTextAnalyzer.Infrastruction.Data;
using AiTextAnalyzer.Infrastruction.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Infrastruction.Vector
{
    public class PgVectorStore : IVectorStore
    {
        private readonly VectorDbContext _db;

        public PgVectorStore(VectorDbContext db)
        {
            _db = db;
        }

        public async Task StoreChunkAsync(string content, float[] embedding, CancellationToken ct)
        {
            _db.Chunks.Add(new DocumentChunk
            {
                Content = content,
                Embedding = new Pgvector.Vector(embedding)
            });

            await _db.SaveChangesAsync(ct);
        }

        public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(float[] embedding, int topK, CancellationToken ct)
        {
            var v = new Pgvector.Vector(embedding);

            return await _db.Chunks
                .OrderBy(x => x.Embedding.CosineDistance(v))
                .Select(x => new VectorSearchResult(
                    x.Id,
                    x.Content,
                    x.Embedding.CosineDistance(v)
                ))
                .Take(topK)
                .ToListAsync(ct);
        }
    }
}
