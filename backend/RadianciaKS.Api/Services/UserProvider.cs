using System.Security.Claims;
using RadianciaKS.Application.Interfaces;

namespace RadianciaKS.Api.Services
{
    public class UserProvider : IUserProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public Guid? GetTenantId()
        {
            var tenantId = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
            return Guid.TryParse(tenantId, out var guid) ? guid : null;
        }

        public Guid? GetUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userId, out var guid) ? guid : null;
        }

        public string? GetUserRole()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Role)?.Value;
        }
    }
}