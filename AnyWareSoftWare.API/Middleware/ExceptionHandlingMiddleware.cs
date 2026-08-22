using System.Text.Json;
using AnyWareSoftWare.Application.Exceptions;

namespace AnyWareSoftWare.API.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AppException ex)
            {
                await WriteError(context, ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception.");
                await WriteError(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

        private static async Task WriteError(HttpContext context, int statusCode, string message)
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            var payload = JsonSerializer.Serialize(new { error = message });
            await context.Response.WriteAsync(payload);
        }
    }
}
