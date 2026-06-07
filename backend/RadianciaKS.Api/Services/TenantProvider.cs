using RadianciaKS.Application.Interfaces;

namespace RadianciaKS.Api.Services
{
    public class TenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _context;

        public TenantProvider(IHttpContextAccessor context)
        {
            _context = context;
        }

        public Guid GetTenantId()
        {
            var headerValue = _context.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();

            if (string.IsNullOrEmpty(headerValue))
                throw new ArgumentException("Tenant não informado.");

            if (!Guid.TryParse(headerValue, out var tenantId))
                throw new ArgumentException("Formato de Tenant inválido.");

            return tenantId;
        }
    }
}