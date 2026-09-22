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
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

            if (path.StartsWith("/api/auth") ||
                path.StartsWith("/swagger") ||
                path.StartsWith("/hubs/") ||
                context.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
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