using AiTextAnalyzer.Application.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Infrastruction.AI
{
    public class CachedEmbeddingProvider : IEmbeddingProvider
    {
        private readonly IEmbeddingProvider _inner;
        private readonly ICacheService _cache;

        public CachedEmbeddingProvider(IEmbeddingProvider inner, ICacheService cache)
        {
            _inner = inner;
            _cache = cache;
        }

        public async Task<float[]> CreateEmbeddingAsync(string input, CancellationToken ct)
        {
            var key = "emb:" + Sha256(input);

            var cached = await _cache.GetAsync<float[]>(key, ct);
            if (cached is not null)
                return cached;

            var emb = await _inner.CreateEmbeddingAsync(input, ct);
            await _cache.SetAsync(key, emb, TimeSpan.FromDays(7), ct);
            return emb;
        }

        private static string Sha256(string s)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(s ?? ""));
            return Convert.ToHexString(bytes);
        }
    }
}
