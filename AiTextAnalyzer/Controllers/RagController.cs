using AiTextAnalyzer.Data;
using AiTextAnalyzer.Models;
using AiTextAnalyzer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace AiTextAnalyzer.Controllers
{
    [EnableRateLimiting("rag")]
    [Authorize]
    [ApiController]
    [Route("api/rag")]
    public class RagController : ControllerBase
    {
        private readonly RagService _rag;

        public RagController(RagService rag) => _rag = rag;

        [HttpPost("ask")]
        public async Task<ActionResult<RagAskResponse>> Ask([FromBody] RagAskRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Question))
                return BadRequest("Question is required.");

            var result = await _rag.AskAsync(req.Question, ct);
            return Ok(result);
        }

        [HttpGet("latest")]
        public async Task<IActionResult> Latest([FromServices] VectorDbContext db, CancellationToken ct)
        {
            var logs = await db.RagLogs
                .OrderByDescending(x => x.CreatedUtc)
                .Take(20)
                .Select(x => new {
                    x.Id,
                    x.CreatedUtc,
                    x.Question,
                    x.Answer,
                    x.CitationsCsv
                })
                .ToListAsync(ct);

            return Ok(logs);
        }
    }
}
