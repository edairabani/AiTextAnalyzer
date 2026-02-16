using AiTextAnalyzer.Data;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace AiTextAnalyzer.Services
{
    public class EmbeddingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly VectorDbContext _db;

        public EmbeddingService(
            IHttpClientFactory httpClientFactory,
            VectorDbContext db)
        {
            _httpClientFactory = httpClientFactory;
            _db = db;
        }

       
        public async Task<float[]> CreateEmbeddingAsync(string text, CancellationToken ct)
        {
            var client = _httpClientFactory.CreateClient("OpenAI");

            var payload = new { model = "text-embedding-3-small", input = text };
            var json = JsonSerializer.Serialize(payload);

            using var res = await client.PostAsync(
                "v1/embeddings",
                new StringContent(json, Encoding.UTF8, "application/json"),
                ct);

            res.EnsureSuccessStatusCode();

            var body = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);

            return doc.RootElement.GetProperty("data")[0].GetProperty("embedding")
                .EnumerateArray()
                .Select(x => x.GetSingle())
                .ToArray();
        }


       
        public async Task StoreChunkAsync(string text, CancellationToken ct)
        {
            var embedding = await CreateEmbeddingAsync(text, ct);

            _db.Chunks.Add(new DocumentChunk
            {
                Content = text,
                Embedding = new Vector(embedding)
            });

            await _db.SaveChangesAsync(ct);
        }


        public async Task<List<DocumentChunk>> SearchAsync(string query, int top = 5, CancellationToken ct = default)
        {
            var embedding = await CreateEmbeddingAsync(query, ct);
            var vector = new Pgvector.Vector(embedding);

            return await _db.Chunks
                .OrderBy(x => x.Embedding.CosineDistance(vector))
                .Take(top)
                .ToListAsync(ct);
        }

        public async Task<List<object>> SearchWithScoreAsync(string query, int top, CancellationToken ct)
        {
            var embedding = await CreateEmbeddingAsync(query, ct);
            var vector = new Pgvector.Vector(embedding);

            return await _db.Chunks
                .OrderBy(x => x.Embedding.CosineDistance(vector))
                .Select(x => new
                {
                    x.Id,
                    x.Content,
                    distance = x.Embedding.CosineDistance(vector)
                })
                .Take(top)
                .Cast<object>()
                .ToListAsync(ct);
        }

        public async Task<int> StoreDocumentAsync(string text, CancellationToken ct)
        {
            var chunks = TextChunker.Chunk(text);

            foreach (var chunk in chunks)
                await StoreChunkAsync(chunk, ct);

            return chunks.Count;
        }

        public async Task<List<(DocumentChunk chunk, double distance)>> SearchWithDistanceAsync(
            string query, int top, CancellationToken ct)
        {
            var embedding = await CreateEmbeddingAsync(query, ct);
            var vector = new Pgvector.Vector(embedding);

            return await _db.Chunks
                .OrderBy(x => x.Embedding.CosineDistance(vector))
                .Select(x => new ValueTuple<DocumentChunk, double>(
                    x,
                    x.Embedding.CosineDistance(vector)))
                .Take(top)
                .ToListAsync(ct);
        }
    }
}
