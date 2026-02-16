namespace AiTextAnalyzer.Services
{
    public static class TextChunker
    {
        // Ziel: ~800–1200 Zeichen pro Chunk, mit etwas Overlap
        public static List<string> Chunk(string text, int maxChars = 1000, int overlap = 150)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0) return new();

            // grob nach Absätzen/Sätzen splitten
            var parts = text
                .Replace("\r\n", "\n")
                .Split(new[] { "\n\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToList();

            var chunks = new List<string>();
            var current = "";

            foreach (var p in parts)
            {
                if (current.Length + p.Length + 1 <= maxChars)
                {
                    current = current.Length == 0 ? p : current + " " + p;
                }
                else
                {
                    if (current.Length > 0)
                        chunks.Add(current);

                    // Overlap vom Ende des letzten Chunks mitnehmen
                    if (overlap > 0 && current.Length > overlap)
                        current = current[^overlap..] + " " + p;
                    else
                        current = p;
                }
            }

            if (current.Length > 0)
                chunks.Add(current);

            return chunks;
        }
    }
}
