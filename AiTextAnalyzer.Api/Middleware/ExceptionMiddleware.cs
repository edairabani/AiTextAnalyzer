using System.Text.Json;

namespace AiTextAnalyzer.Api.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // Client canceled the request
                context.Response.StatusCode = StatusCodes.Status499ClientClosedRequest; // nginx style
            }
            catch (Exception ex)
            {
                var traceId = context.TraceIdentifier;

                _logger.LogError(ex, "Unhandled exception. TraceId={TraceId}", traceId);

                context.Response.ContentType = "application/problem+json";
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;

                var problem = new
                {
                    type = "https://httpstatuses.com/500",
                    title = "Unexpected error",
                    status = 500,
                    traceId,
                    detail = "An unexpected error occurred. Use traceId when contacting support."
                };

                var json = JsonSerializer.Serialize(problem);
                await context.Response.WriteAsync(json);
            }
        }
    }
}
