using RadianciaKS.Domain.Interfaces;

namespace RadianciaKS.Domain.Models
{
    public class AuditLog : IMustHaveTenant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Guid? UserId { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
    }
}