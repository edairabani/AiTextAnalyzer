using AiTextAnalyzer.Data;
using AiTextAnalyzer.Models;
using System.Text;
using System.Text.Json;

namespace AiTextAnalyzer.Services
{
    public class OpenAiAnalyzeService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _db;
        private readonly ILogger<OpenAiAnalyzeService> _logger;

        public OpenAiAnalyzeService(IHttpClientFactory httpClientFactory, AppDbContext db, ILogger<OpenAiAnalyzeService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _db = db;
            _logger = logger;
        }

        public async Task<AnalyzeResult> AnalyzeAndStoreAsync(string text, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text is required.", nameof(text));

            var client = _httpClientFactory.CreateClient("OpenAI");

            var payload = new
            {
                model = "gpt-4.1-mini",
                temperature = 0.2,
                response_format = new { type = "json_object" }, // wichtig: JSON erzwingen
                messages = new object[]
                {
                new { role = "system", content =
                    "Analyze the user's text. Return ONLY valid JSON with fields: sentiment, category, summary." },
                new { role = "user", content = text }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Calling OpenAI analyze endpoint...");
            using var resp = await client.PostAsync("v1/chat/completions", content, ct);

            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI error: {Status} {Body}", (int)resp.StatusCode, body);
                resp.EnsureSuccessStatusCode();
            }

            using var doc = JsonDocument.Parse(body);
            var textJson = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrWhiteSpace(textJson))
                throw new InvalidOperationException("Empty content from OpenAI.");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<AnalyzeResult>(textJson, options)
                         ?? throw new InvalidOperationException("Invalid JSON from OpenAI.");

            // DB speichern
            _db.Analyses.Add(new AnalyzeRecord
            {
                InputText = text,
                Sentiment = result.Sentiment,
                Category = result.Category,
                Summary = result.Summary
            });

            await _db.SaveChangesAsync(ct);

            return result;
        }
    }
}
