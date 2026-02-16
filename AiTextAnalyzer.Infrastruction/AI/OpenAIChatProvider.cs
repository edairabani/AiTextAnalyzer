using AiTextAnalyzer.Application.AI;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Infrastruction.AI
{
    public class OpenAIChatProvider : IChatProvider
    {
        private readonly IHttpClientFactory _factory;

        public OpenAIChatProvider(IHttpClientFactory factory, IConfiguration cfg)
        {
            _factory = factory;
        }

        public async Task<string> CompleteJsonAsync(string systemPrompt, string userPrompt, CancellationToken ct)
        {
            var client = _factory.CreateClient("OpenAI");

            var payload = new
            {
                model = "gpt-4.1-mini",
                temperature = 0.2,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
                }
            };

            var json = JsonSerializer.Serialize(payload);

            using var res = await client.PostAsync(
                "v1/chat/completions",
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
                throw new InvalidOperationException("Empty model response.");

            return content; // muss JSON sein
        }
    }
}
