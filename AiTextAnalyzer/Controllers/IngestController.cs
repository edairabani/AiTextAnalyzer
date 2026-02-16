using AiTextAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiTextAnalyzer.Controllers
{
    [ApiController]
    [Route("api/ingest")]
    public class IngestController : ControllerBase
    {
        private readonly EmbeddingService _service;

        public IngestController(EmbeddingService service) => _service = service;

        public record IngestRequest(string Text);

        [HttpPost]
        public async Task<IActionResult> Ingest([FromBody] IngestRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Text))
                return BadRequest("Text is required.");

            var count = await _service.StoreDocumentAsync(req.Text, ct);
            return Ok(new { chunksStored = count });
        }
    }
}
