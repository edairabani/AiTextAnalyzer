using AiTextAnalyzer.Application.Search;
using Microsoft.AspNetCore.Mvc;

namespace AiTextAnalyzer.Api.Controllers
{
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase
    {
        private readonly SearchChunks _search;
        public SearchController(SearchChunks search) => _search = search;

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int topK = 5, CancellationToken ct = default)
        {
            var res = await _search.HandleAsync(query, topK, ct);
            return Ok(res);
        }
    }
}
