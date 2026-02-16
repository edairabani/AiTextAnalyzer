namespace AiTextAnalyzer.Services
{
    using AiTextAnalyzer.Models;
    using OpenAI;
    using OpenAI.Chat;
    using System.Data;
    using System.Text.Json;

    public class OpenAiService
    {
        private readonly ChatClient _chat;

        public OpenAiService(IConfiguration config)
        {
            var apiKey = config["OpenAI:ApiKey"];
           
            _chat = new ChatClient("gpt-4.1-mini", apiKey);
        }

        public async Task<AnalyzeResult> AnalyzeText(string text)
        {
            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("""
                You analyze the user's text.
                Return ONLY valid JSON with exactly these fields:
                sentiment, category, summary
                No markdown. No extra text.
                """),
                new UserChatMessage(text)
            };

            var chatOptions = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 200
            };

            var result = await _chat.CompleteChatAsync(messages, chatOptions);

            

            var json = result.Value.Content[0].Text;

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<AnalyzeResult>(json, options);

            if (parsed is null)
                throw new InvalidOperationException("OpenAI returned invalid JSON.");

            return parsed;
        }
    }

}
