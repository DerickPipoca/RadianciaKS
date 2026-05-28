using System.Net;
using System.Text.Json;

namespace RadianciaKS.Api.Middlewares
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

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu uma exceção não tratada na API.");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext httpContext, Exception ex)
        {
            httpContext.Response.ContentType = "applicaton/json";

            if (ex is ArgumentException)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;

                var result = JsonSerializer.Serialize(new { error = ex.Message });
                return httpContext.Response.WriteAsync(result);
            }

            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var internalResult = JsonSerializer.Serialize(new { error = "Ocorreu um erro interno no servidor. Por favor, tente novamente mais tarde." });
            return httpContext.Response.WriteAsync(internalResult);
        }
    }
}