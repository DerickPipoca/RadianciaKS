namespace RadianciaKS.Domain.Interfaces
{
    public interface IMustHaveTenant
    {
        public Guid TenantId { get; set; }
    }
}