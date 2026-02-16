using AiTextAnalyzer.Application.Ingest;
using Microsoft.AspNetCore.Mvc;

namespace AiTextAnalyzer.Api.Controllers
{
    [ApiController]
    [Route("api/documents")]
    public class DocumentController : ControllerBase
    {
        private readonly StoreChunk _storeChunk;
        private readonly IngestDocument _ingest;

        public DocumentController(StoreChunk storeChunk, IngestDocument ingest)
        {
            _storeChunk = storeChunk;
            _ingest = ingest;
        }

        public record StoreChunkRequest(string Text);
        public record IngestRequestFromDocument(string Text);

        // entspricht deinem bisherigen /chunk
        [HttpPost("chunk")]
        public async Task<IActionResult> StoreChunk([FromBody] StoreChunkRequest req, CancellationToken ct)
        {
            await _storeChunk.HandleAsync(req.Text, ct);
            return Ok();
        }

        // optional: komplettes Dokument
        [HttpPost("ingest")]
        public async Task<IActionResult> Ingest([FromBody] IngestRequestFromDocument req, CancellationToken ct)
        {
            var count = await _ingest.HandleAsync(req.Text, ct);
            return Ok(new { chunksStored = count });
        }
    }
}
