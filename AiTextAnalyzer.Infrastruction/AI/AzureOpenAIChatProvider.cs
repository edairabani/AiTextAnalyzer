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
    public class AzureOpenAIChatProvider : IChatProvider
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;

        public AzureOpenAIChatProvider(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _cfg = cfg;
        }

        public async Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct)
        {
            var deployment = _cfg["AI:AzureOpenAI:ChatDeployment"]!;
            // many Azure setups still require api-version on the classic endpoint; v1 exists in Foundry. :contentReference[oaicite:5]{index=5}
            var apiVersion = _cfg["AI:AzureOpenAI:ApiVersion"] ?? "2024-10-21";

            var payload = new
            {
                temperature = 0.2,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
                }
            };

            var json = JsonSerializer.Serialize(payload);

            using var res = await _http.PostAsync(
                $"/openai/deployments/{deployment}/chat/completions?api-version={apiVersion}",
                new StringContent(json, Encoding.UTF8, "application/json"),
                ct);

            res.EnsureSuccessStatusCode();

            var body = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);

            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Empty Azure OpenAI response.");

            return content;
        }
    }
}
