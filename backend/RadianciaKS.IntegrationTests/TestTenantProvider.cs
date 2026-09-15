using RadianciaKS.Application.Interfaces;

namespace RadianciaKS.IntegrationTests
{
    public class TestTenantProvider : ITenantProvider
    {
        public static Guid DefaultTenantId { get; set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public Guid GetTenantId() => DefaultTenantId;
    }
}