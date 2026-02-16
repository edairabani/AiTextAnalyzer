using AiTextAnalyzer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiTextAnalyzer.Controllers
{
    [ApiController]
    [Route("api/chunks")]
    public class ChunksController : ControllerBase
    {
        private readonly VectorDbContext _db;

        public ChunksController(VectorDbContext db)
        {
            _db = db;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id, CancellationToken ct)
        {
            var chunk = await _db.Chunks
                .Where(x => x.Id == id)
                .Select(x => new { x.Id, x.Content })
                .FirstOrDefaultAsync(ct);

            if (chunk == null)
                return NotFound();

            return Ok(chunk);
        }
    }
}
