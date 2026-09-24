using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.Interfaces;

namespace RadianciaKS.Api.Middlewares
{
    public class LicenseValidationMiddleware
    {
        private readonly RequestDelegate _next;

        public LicenseValidationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IApplicationDbContext dbContext)
        {
            var rawPath = context.Request.Path.Value ?? string.Empty;
            // Garante que o path comece com barra e esteja em minúsculo
            var path = rawPath.StartsWith('/') ? rawPath.ToLowerInvariant() : $"/{rawPath.ToLowerInvariant()}";

            if (HttpMethods.IsOptions(context.Request.Method) ||
                path.StartsWith("/api/auth") ||
                path.StartsWith("/swagger") ||
                path.StartsWith("/hubs") ||
                path.StartsWith("/health"))
            {
                await _next(context);
                return;
            }

            var license = await dbContext.SystemLicenses.AsNoTracking().FirstOrDefaultAsync();

            if (license == null || !license.IsValid())
            {
                context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
                context.Response.ContentType = "application/json";

                var responsePayload = new
                {
                    statusCode = StatusCodes.Status402PaymentRequired,
                    error = "Payment Required",
                    message = "A licença do sistema está expirada ou suspensa. Contacte o suporte comercial para regularizar o acesso.",
                    status = license?.Status.ToString() ?? "NOT_FOUND"
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(responsePayload));
                return;
            }

            await _next(context);
        }
    }
}