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
    public class OllamaChatProvider : IChatProvider
    {
        private readonly HttpClient _http;
        private readonly IConfiguration _cfg;

        public OllamaChatProvider(HttpClient http, IConfiguration cfg)
        {
            _http = http;
            _cfg = cfg;
        }

        public async Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct)
        {
            var model = _cfg["AI:Ollama:ChatModel"] ?? "llama3.1";

            // Ollama expects messages for /api/chat. :contentReference[oaicite:1]{index=1}
            var payload = new
            {
                model,
                stream = false,
                messages = new object[]
                {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var res = await _http.PostAsync(
                "/api/chat",
                new StringContent(json, Encoding.UTF8, "application/json"),
                ct);

            res.EnsureSuccessStatusCode();

            var body = await res.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(body);

            // response.message.content is the assistant text in Ollama chat response. :contentReference[oaicite:2]{index=2}
            var content = doc.RootElement.GetProperty("message").GetProperty("content").GetString();
            if (string.IsNullOrWhiteSpace(content))
                throw new InvalidOperationException("Empty Ollama response.");

            return content; // must be JSON string per your RAG prompt
        }
    }
}
