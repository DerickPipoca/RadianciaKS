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
            httpContext.Response.ContentType = "application/json";

            string message;
            int statusCode;

            switch (ex)
            {
                case FluentValidation.ValidationException validationException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = validationException.Errors.FirstOrDefault()?.ErrorMessage ?? "Erro de validação.";
                    break;

                case ArgumentException argumentException:
                    statusCode = (int)HttpStatusCode.BadRequest;
                    message = argumentException.Message;
                    break;

                case UnauthorizedAccessException unauthorizedException:
                    statusCode = (int)HttpStatusCode.Forbidden;
                    message = unauthorizedException.Message;
                    break;

                default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    message = "Ocorreu um erro interno no servidor. Por favor, tente novamente mais tarde.";
                    break;
            }

            httpContext.Response.StatusCode = statusCode;
            var result = JsonSerializer.Serialize(new { error = message });

            return httpContext.Response.WriteAsync(result);
        }
    }
}