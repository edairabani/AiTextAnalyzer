using AiTextAnalyzer.Models;
using Azure;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace AiTextAnalyzer.Controllers
{
    [ApiController]
    [Route("api/stream")]
    public class StreamController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<StreamController> _logger;

        public StreamController(IHttpClientFactory httpClientFactory, ILogger<StreamController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        [HttpPost]
        public async Task Stream([FromBody] AnalyzeRequest req, CancellationToken ct)
        {
            Response.Headers.ContentType = "text/event-stream; charset=utf-8";
            Response.Headers.CacheControl = "no-cache";

            var client = _httpClientFactory.CreateClient("OpenAI");

            var payload = new
            {
                model = "gpt-4.1-mini",
                temperature = 0.2,
                stream = true,
                messages = new object[]
                {
                new { role = "system", content = "You are helpful. Stream your answer." },
                new { role = "user", content = req.Text }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
            {
                Content = content
            };

            _logger.LogInformation("Streaming from OpenAI...");
            using var resp = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream && !ct.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // OpenAI stream: lines begin with "data: ..."
                if (!line.StartsWith("data:")) continue;

                var data = line.Substring("data:".Length).Trim();

                if (data == "[DONE]") break;

                // data ist JSON pro Chunk
                // choices[0].delta.content enthält Textstück
                try
                {
                    using var chunk = JsonDocument.Parse(data);
                    var root = chunk.RootElement;

                    var delta = root.GetProperty("choices")[0].GetProperty("delta");
                    if (delta.TryGetProperty("content", out var contentEl))
                    {
                        var token = contentEl.GetString();
                        if (!string.IsNullOrEmpty(token))
                        {
                            await Response.WriteAsync($"data: {token}\n\n", ct);
                            await Response.Body.FlushAsync(ct);
                        }
                    }
                }
                catch
                {
                    // manche Zeilen können unerwartet sein -> ignorieren
                }
            }
        }
    }
}
