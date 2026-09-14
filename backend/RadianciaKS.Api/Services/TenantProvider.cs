using RadianciaKS.Application.Interfaces;

namespace RadianciaKS.Api.Services
{
    public class TenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _context;
        private readonly IConfiguration _configuration;

        public TenantProvider(IHttpContextAccessor context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public Guid GetTenantId()
        {
            var httpContext = _context.HttpContext;

            if (httpContext != null)
            {
                if (httpContext.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) &&
                    Guid.TryParse(tenantHeader, out var tenantIdFromHeader))
                {
                    return tenantIdFromHeader;
                }

                var envTenant = _configuration["TENANT_ID"] ?? Environment.GetEnvironmentVariable("TENANT_ID");
                if (!string.IsNullOrEmpty(envTenant) && Guid.TryParse(envTenant, out var fallbackTenantId))
                {
                    return fallbackTenantId;
                }

                throw new ArgumentException("Tenant não informado.");
            }

            var startupTenant = _configuration["TENANT_ID"] ?? Environment.GetEnvironmentVariable("TENANT_ID");
            if (!string.IsNullOrEmpty(startupTenant) && Guid.TryParse(startupTenant, out var id))
            {
                return id;
            }

            throw new ArgumentException("Tenant não informado na inicialização do sistema.");
        }
    }
}