using AiTextAnalyzer.Application.AI;
using Microsoft.AspNetCore.Mvc;

namespace AiTextAnalyzer.Api.Controllers
{
    [ApiController]
    [Route("api/ai")]
    public class AiController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IChatProvider _chat;
        private readonly IEmbeddingProvider _embeddings;
        private readonly ILogger<AiController> _logger;

        public AiController(
            IConfiguration config,
            IChatProvider chat,
            IEmbeddingProvider embeddings,
             ILogger<AiController> logger
            )
        {
            _logger = logger;
            _config = config;
            _chat = chat;
            _embeddings = embeddings;
        }

        // Shows which provider is active
        [HttpGet("provider")]
        public IActionResult Provider()
        {
            var provider = _config["AI:Provider"] ?? "OpenAI";
            return Ok(new { provider });
        }

        // Simple smoke test for chat + embeddings
        [HttpGet("ping")]
        public async Task<IActionResult> Ping(CancellationToken ct)
        {
            var emb = await _embeddings.CreateEmbeddingAsync("ping", ct);

            var json = await _chat.CompleteJsonAsync(
                "Return only JSON: {\"ok\": true}",
                "ping",
                ct);

            return Ok(new
            {
                embeddingSize = emb.Length,
                chatResponse = json
            });
        }
    }
}
