using AiTextAnalyzer.Application.AI;
using AiTextAnalyzer.Application.Vector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Application.Rag
{
    public class RagService : IRagService
    {
        private readonly IEmbeddingProvider _embeddings;
        private readonly IVectorStore _store;
        private readonly IChatProvider _chat;
        private readonly IRagLogRepository _logs;
        private readonly ICacheService _cache;

        public RagService(
            IEmbeddingProvider embeddings,
            IVectorStore store,
            IChatProvider chat,
            IRagLogRepository logs,
            ICacheService cache)
        {
            _cache = cache;
            _embeddings = embeddings;
            _store = store;
            _chat = chat;
            _logs = logs;
        }

        public async Task<RagDTO> AskAsync(string question, int topK, double threshold, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("Question is required.", nameof(question));

            var qKey = "rag:" + question.Trim().ToLowerInvariant();
            var cached = await _cache.GetAsync<RagDTO>(qKey, ct);
            if (cached is not null)
                return cached;

            var qEmb = await _embeddings.CreateEmbeddingAsync(question, ct);
            var results = await _store.SearchAsync(qEmb, topK, ct);

            var good = results.Where(r => r.Distance <= threshold).ToList();
            if (good.Count == 0)
                return new RagDTO("I don't know based on the provided documents.", Array.Empty<RagCitationDTO>());

            var allowedById = good.ToDictionary(x => x.Id, x => x.Content);
            var distanceById = good.ToDictionary(x => x.Id, x => x.Distance);

            var ctx = new StringBuilder();
            foreach (var r in good)
                ctx.AppendLine($"[ChunkId:{r.Id}] {r.Content}");

            var system =
                """
            You are a RAG assistant.
            Answer ONLY using the provided context chunks.
            If the answer is not in the context, say: "I don't know based on the provided documents."
            Return ONLY valid JSON with fields:
            - answer (string)
            - citations (array of integers: chunk ids used)
            No markdown, no extra text.
            """;

            var user = $"QUESTION:\n{question}\n\nCONTEXT:\n{ctx}";
            var json = await _chat.CompleteJsonAsync(system, user, ct);

            using var outDoc = JsonDocument.Parse(json);
            var root = outDoc.RootElement;

            var answer = root.GetProperty("answer").GetString() ?? "";

            var modelIds = root.TryGetProperty("citations", out var cits)
                ? cits.EnumerateArray().Select(x => x.GetInt32()).ToArray()
                : Array.Empty<int>();

            var allowedIds = allowedById.Keys.ToHashSet();
            var safeIds = modelIds.Where(id => allowedIds.Contains(id)).Distinct().ToArray();
            if (safeIds.Length == 0)
                safeIds = good.Take(2).Select(x => x.Id).ToArray();

            var citations = safeIds
                .Select(id => new RagCitationDTO(
                    id,
                    BestSnippet(allowedById[id], question),
                    distanceById.TryGetValue(id, out var d) ? d : double.NaN
                ))
                .OrderBy(c => c.Distance)
                .ToArray();

            answer = $"{answer}\n\nSources: [{string.Join(", ", citations.Select(c => c.Id))}]";

            await _logs.SaveAsync(question, answer, citations.Select(c => c.Id), ct);

            var ragDTOAnswer = new RagDTO(answer, citations);
            await _cache.SetAsync(qKey, ragDTOAnswer, TimeSpan.FromMinutes(10), ct);

            return ragDTOAnswer;
        }

        private static string BestSnippet(string chunk, string question, int max = 220)
        {
            static string Shorten(string s, int m)
            {
                s = (s ?? "").Trim();
                return s.Length <= m ? s : s.Substring(0, m) + "…";
            }

            chunk = (chunk ?? "").Trim();
            if (chunk.Length == 0) return "";

            var q = (question ?? "").ToLowerInvariant();

            var sentences = chunk
                .Replace("\r\n", "\n")
                .Split(new[] { ". ", ".\n", "!\n", "?\n", "!", "?" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToList();

            if (sentences.Count == 0) return Shorten(chunk, max);

            int Score(string s)
            {
                var t = s.ToLowerInvariant();
                var qWords = q.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct();
                return qWords.Count(w => w.Length > 3 && t.Contains(w));
            }

            var best = sentences.OrderByDescending(Score).ThenByDescending(s => s.Length).First();
            return Shorten(best, max);
        }
    }
}
