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
    public class OllamaEmbeddingProvider : IEmbeddingProvider
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;

        public OllamaEmbeddingProvider(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _cfg = cfg;
        }

        public async Task<float[]> CreateEmbeddingAsync(string input, CancellationToken ct)
        {
            var model = _cfg["AI:Ollama:EmbeddingModel"] ?? "nomic-embed-text";

            // Ollama embeddings endpoint. :contentReference[oaicite:3]{index=3}
            var payload = new { model, prompt = input };
            var json = JsonSerializer.Serialize(payload);

            using var res = await _http.PostAsync(
                "/api/embeddings",
                new StringContent(json, Encoding.UTF8, "application/json"),
                ct);

            res.EnsureSuccessStatusCode();

            var body = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);

            return doc.RootElement.GetProperty("embedding")
                .EnumerateArray().Select(x => x.GetSingle()).ToArray();
        }
    }
}
