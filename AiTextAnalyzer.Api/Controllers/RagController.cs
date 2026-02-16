using AiTextAnalyzer.Application.Rag;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace AiTextAnalyzer.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/rag")]
    public class RagController : ControllerBase
    {
        private readonly IRagService _rag;
        private readonly IConfiguration _config;

        public RagController(IRagService rag, IConfiguration config)
        {
            _rag = rag;
            _config = config;
        }

        public record RagAskRequest(string Question);
        public record RagAskResponse(string Answer, RagCitationDTO[] Citations);

        [HttpPost("ask")]
        public async Task<ActionResult<RagAskResponse>> Ask([FromBody] RagAskRequest req, CancellationToken ct)
        {
            var topK = _config.GetValue("Rag:TopK", 8);
            var threshold = _config.GetValue("Rag:DistanceThreshold", 0.65);

            var result = await _rag.AskAsync(req.Question, topK, threshold, ct);
            return Ok(result);
        }
    }
}
