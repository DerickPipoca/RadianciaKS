using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

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

            var problemDetails = new ProblemDetails
            {
                Instance = httpContext.Request.Path
            };

            switch (ex)
            {
                case ValidationException validationException:
                    problemDetails.Status = (int)HttpStatusCode.BadRequest;
                    problemDetails.Title = "Erro de Validação";
                    problemDetails.Detail = "Um ou mais campos contêm erros.";

                    var validationErrors = validationException.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage).ToArray()
                        );

                    problemDetails.Extensions.Add("errors", validationErrors);
                    break;

                case ArgumentException argumentException:
                    problemDetails.Status = (int)HttpStatusCode.BadRequest;
                    problemDetails.Title = "Requisição Inválida";
                    problemDetails.Detail = argumentException.Message;
                    break;

                case UnauthorizedAccessException unauthorizedException:
                    problemDetails.Status = (int)HttpStatusCode.Forbidden;
                    problemDetails.Title = "Acesso Negado";
                    problemDetails.Detail = unauthorizedException.Message;
                    break;

                default:
                    problemDetails.Status = (int)HttpStatusCode.InternalServerError;
                    problemDetails.Title = "Erro Interno do Servidor";
                    problemDetails.Detail = "Ocorreu um erro interno no servidor. Por favor, tente novamente mais tarde.";
                    break;
            }

            httpContext.Response.StatusCode = problemDetails.Status.Value;

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var result = JsonSerializer.Serialize(problemDetails, options);

            return httpContext.Response.WriteAsync(result);
        }
    }
}