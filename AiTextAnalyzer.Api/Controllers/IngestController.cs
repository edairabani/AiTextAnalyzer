using AiTextAnalyzer.Application.Ingest;
using Microsoft.AspNetCore.Mvc;

namespace AiTextAnalyzer.Api.Controllers
{
    [ApiController]
    [Route("api/ingest")]
    public class IngestController : ControllerBase
    {
        private readonly IngestDocument _ingest;
        public IngestController(IngestDocument ingest) => _ingest = ingest;

        public record IngestRequest(string Text);

        [HttpPost]
        public async Task<IActionResult> Ingest([FromBody] IngestRequest req, CancellationToken ct)
        {
            var count = await _ingest.HandleAsync(req.Text, ct);
            return Ok(new { chunksStored = count });
        }
    }
}
