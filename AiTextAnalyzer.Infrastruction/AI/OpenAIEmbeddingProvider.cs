using AiTextAnalyzer.Application.AI;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Infrastruction.AI
{
    public class OpenAIEmbeddingProvider : IEmbeddingProvider
    {
        private readonly IHttpClientFactory _factory;

        public OpenAIEmbeddingProvider(IHttpClientFactory factory, IConfiguration cfg)
        {
            _factory = factory;
        }

        public async Task<float[]> CreateEmbeddingAsync(string input, CancellationToken ct)
        {
            var client = _factory.CreateClient("OpenAI");

            var payload = new { model = "text-embedding-3-small", input };
            var json = JsonSerializer.Serialize(payload);

            using var res = await client.PostAsync(
                "v1/embeddings",
                new StringContent(json, Encoding.UTF8, "application/json"),
                ct);

            res.EnsureSuccessStatusCode();

            var body = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);

            return doc.RootElement.GetProperty("data")[0].GetProperty("embedding")
                .EnumerateArray().Select(x => x.GetSingle()).ToArray();
        }
    }
}
