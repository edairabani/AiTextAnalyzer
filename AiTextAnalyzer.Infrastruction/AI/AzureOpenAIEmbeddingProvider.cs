using AiTextAnalyzer.Application.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Infrastruction.AI
{
    public class AzureOpenAIEmbeddingProvider : IEmbeddingProvider
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;
        private readonly ILogger<AzureOpenAIEmbeddingProvider> _logger;

        public AzureOpenAIEmbeddingProvider(
            HttpClient http,
            IConfiguration cfg,
            ILogger<AzureOpenAIEmbeddingProvider> logger)
        {
            _logger = logger;
            _http = http;
            _cfg = cfg;
        }

        public async Task<float[]> CreateEmbeddingAsync(string input, CancellationToken ct)
        {
            var deployment = _cfg["AI:AzureOpenAI:EmbeddingDeployment"]!;
            var apiVersion = _cfg["AI:AzureOpenAI:ApiVersion"] ?? "2024-10-21";

            var payload = new { input };
            var json = JsonSerializer.Serialize(payload);

            using var res = await _http.PostAsync(
                $"/openai/deployments/{deployment}/embeddings?api-version={apiVersion}",
                new StringContent(json, Encoding.UTF8, "application/json"),
                ct);

            res.EnsureSuccessStatusCode();


            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI embedding failed. Status={Status} Body={Body}",
                    (int)res.StatusCode, body);
                res.EnsureSuccessStatusCode();
            }

            using var doc = JsonDocument.Parse(body);

            var model = _cfg["AI:OpenAI:EmbeddingModel"] ?? "text-embedding-3-small";
            
            _logger.LogInformation("OpenAI embedding ok. Model={Model} InputLen={InputLen}",
            model, input?.Length ?? 0);

            return doc.RootElement.GetProperty("data")[0].GetProperty("embedding")
                .EnumerateArray().Select(x => x.GetSingle()).ToArray();
        }
    }
}
