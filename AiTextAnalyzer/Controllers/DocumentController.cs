using AiTextAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiTextAnalyzer.Controllers
{
    [ApiController]
    [Route("api/documents")]
    public class DocumentController : ControllerBase
    {
        private readonly EmbeddingService _service;

        public DocumentController(EmbeddingService service) => _service = service;

        public record StoreRequest(string Text);

        [HttpPost("chunk")]
        public async Task<IActionResult> Store([FromBody] StoreRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Text)) return BadRequest("Text is required.");
            await _service.StoreChunkAsync(req.Text, ct);
            return Ok();
        }
    }
}
