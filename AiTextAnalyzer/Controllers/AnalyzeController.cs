using AiTextAnalyzer.Models;
using AiTextAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;

namespace AiTextAnalyzer.Controllers
{
    [ApiController]
    [Route("api/analyze")]
    public class AnalyzeController : ControllerBase
    {
        private readonly OpenAiAnalyzeService _service;

        public AnalyzeController(OpenAiAnalyzeService service) => _service = service;

        [HttpPost]
        public async Task<ActionResult<AnalyzeResult>> Analyze([FromBody] AnalyzeRequest req, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(req.Text))
                return BadRequest("Text is required.");

            var result = await _service.AnalyzeAndStoreAsync(req.Text, ct);
            return Ok(result);
        }
    }

}
