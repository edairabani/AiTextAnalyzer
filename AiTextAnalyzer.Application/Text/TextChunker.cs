using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTextAnalyzer.Application.Text
{
    public static class TextChunker
    {
        public static List<string> Chunk(string text, int maxChars = 1000, int overlap = 150)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0) return new();

            var parts = text.Replace("\r\n", "\n")
                .Split(new[] { "\n\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();

            var chunks = new List<string>();
            var current = "";

            foreach (var p in parts)
            {
                if (current.Length + p.Length + 1 <= maxChars)
                    current = current.Length == 0 ? p : current + " " + p;
                else
                {
                    if (current.Length > 0) chunks.Add(current);
                    current = (overlap > 0 && current.Length > overlap)
                        ? current[^overlap..] + " " + p
                        : p;
                }
            }

            if (current.Length > 0) chunks.Add(current);
            return chunks;
        }
    }
}
