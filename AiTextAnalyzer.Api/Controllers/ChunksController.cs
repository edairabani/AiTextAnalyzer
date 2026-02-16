using AiTextAnalyzer.Application.Chunks;
using Microsoft.AspNetCore.Mvc;

namespace AiTextAnalyzer.Api.Controllers
{
    [ApiController]
    [Route("api/chunks")]
    public class ChunksController : ControllerBase
    {
        private readonly IChunkQuery _query;

        public ChunksController(IChunkQuery query)
        {
            _query = query;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var chunk = await _query.GetByIdAsync(id, ct);
            if (chunk == null) return NotFound();
            return Ok(chunk);
        }
    }
}
