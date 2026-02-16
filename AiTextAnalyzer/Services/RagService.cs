using AiTextAnalyzer.Data;
using AiTextAnalyzer.Models;
using System.Text;
using System.Text.Json;

namespace AiTextAnalyzer.Services
{
    public class RagService
    {
        private readonly EmbeddingService _embeddingService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly VectorDbContext _db;
        private readonly IConfiguration _config;

        public RagService(
            EmbeddingService embeddingService,
            IHttpClientFactory httpClientFactory,
            VectorDbContext db,
            IConfiguration config)
        {
            _embeddingService = embeddingService;
            _httpClientFactory = httpClientFactory;
            _db = db;
            _config = config;
        }

        public async Task<RagAskResponse> AskAsync(string question, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("Question is required.", nameof(question));

            // 1) Retrieve top chunks + threshold
            var topK = _config.GetValue("Rag:TopK", 8);
            var threshold = _config.GetValue("Rag:DistanceThreshold", 0.65);

            var results = await _embeddingService.SearchWithDistanceAsync(question, top: topK, ct: ct);
            var distanceById = results.ToDictionary(r => r.chunk.Id, r => r.distance);

            var good = results
                .Where(r => r.distance <= threshold)
                .Select(r => r.chunk)
                .ToList();

            if (good.Count == 0)
                return new RagAskResponse(
                    "I don't know based on the provided documents.",
                    Array.Empty<RagCitation>());

            // 2) Context build (ONLY good chunks) + map for snippets
            var allowedChunksById = good.ToDictionary(c => c.Id, c => c.Content);

            var contextBuilder = new StringBuilder();
            foreach (var c in good)
            {
                contextBuilder.AppendLine($"[ChunkId:{c.Id}] {c.Content}");
            }

            var client = _httpClientFactory.CreateClient("OpenAI");

            var languageInstruction = LooksGerman(question)
            ? "Answer in German."
            : "Answer in English.";

            // 3) Ask LLM with forced JSON output
            var payload = new
            {
                model = "gpt-4.1-mini",
                temperature = 0.2,
                response_format = new { type = "json_object" },
                messages = new object[]
                {
                    new {
                        role = "system",
                        content =
                                $"""
                                You are a RAG assistant.
                                {languageInstruction}
                                Answer ONLY using the provided context chunks.
                                If the answer is not in the context, say: "I don't know based on the provided documents."
                                Return ONLY valid JSON with fields:
                                - answer (string)
                                - citations (array of integers: chunk ids used)
                                No markdown, no extra text.
                                """
                    },
                    new { role = "user", content = $"QUESTION:\n{question}\n\nCONTEXT:\n{contextBuilder}" }
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

            using var outDoc = JsonDocument.Parse(content);
            var root = outDoc.RootElement;

            var answer = root.GetProperty("answer").GetString() ?? "";

            var modelCitations = root.TryGetProperty("citations", out var citationsEl)
                ? citationsEl.EnumerateArray().Select(x => x.GetInt32()).ToArray()
                : Array.Empty<int>();

            // 4) Validate citations (anti-hallucination) + fallback
            var allowedIds = allowedChunksById.Keys.ToHashSet();

            var safeIds = modelCitations
                .Where(id => allowedIds.Contains(id))
                .Distinct()
                .ToArray();

            if (safeIds.Length == 0 && good.Count > 0)
                safeIds = good.Take(2).Select(c => c.Id).ToArray();

            // 5) Build snippet objects
            static string Shorten(string s, int max = 180)
            {
                s = (s ?? "").Trim();
                return s.Length <= max ? s : s.Substring(0, max) + "…";
            }

            var citationObjects = safeIds
             .Select(id => new RagCitation(id,
                                           BestSnippet(allowedChunksById[id], question),
                                           distanceById.TryGetValue(id, out var d) ? d : double.NaN))
             .ToArray();

            var sourceIds = citationObjects.Select(c => c.Id);

            // 6) Save log (IDs only)
            _db.RagLogs.Add(new RagQueryLog
            {
                Question = question,
                Answer = $"{answer}\n\nSources: [{string.Join(", ", sourceIds)}]",
                CitationsCsv = string.Join(",", citationObjects.Select(c => c.Id))
            });

            await _db.SaveChangesAsync(ct);

            return new RagAskResponse($"{answer}\n\nSources: [{string.Join(", ", sourceIds)}]", citationObjects);
        }


        static string BestSnippet(string chunk, string question, int max = 220)
        {
            chunk = (chunk ?? "").Trim();
            if (chunk.Length == 0) return "";

            var q = (question ?? "").ToLowerInvariant();

            // sehr simples Satz-Splitting
            var sentences = chunk
                .Replace("\r\n", "\n")
                .Split(new[] { ". ", ".\n", "!\n", "?\n", "!", "?" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            if (sentences.Count == 0)
                return Shorten(chunk, max);

            int Score(string s)
            {
                var t = s.ToLowerInvariant();
                var qWords = q.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct();

                // gemeinsame Wörter zählen (ignoriert sehr kurze Wörter)
                return qWords.Count(w => w.Length > 3 && t.Contains(w));
            }

            var best = sentences
                .OrderByDescending(Score)
                .ThenByDescending(s => s.Length) // falls Score gleich
                .First();

            return Shorten(best, max);
        }

        static string Shorten(string s, int max)
        {
            s = (s ?? "").Trim();
            return s.Length <= max ? s : s.Substring(0, max) + "…";
        }

        static bool LooksGerman(string text)
        {
            text = (text ?? "").ToLowerInvariant();
            if (text.Contains("ä") || text.Contains("ö") || text.Contains("ü") || text.Contains("ß"))
                return true;

            // einfache Heuristik über häufige Wörter
            var markers = new[] { " wie ", " was ", " warum ", " wieso ", " kann ", " ich ", " bitte ", " zurücksetzen", "passwort" };
            return markers.Any(m => text.Contains(m));
        }

    }
}
