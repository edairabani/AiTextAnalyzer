using AiTextAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiTextAnalyzer.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly EmbeddingService _service;

        public SearchController(EmbeddingService service) => _service = service;

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string query, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(query)) return BadRequest("query is required.");

            var results = await _service.SearchWithScoreAsync(query, top: 5, ct: ct);
            return Ok(results);
        }
    }
}
