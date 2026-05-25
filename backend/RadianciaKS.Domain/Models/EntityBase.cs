using RadianciaKS.Domain.Interfaces;

namespace RadianciaKS.Domain.Models
{
    public abstract class EntityBase : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedAt { get; private set; }

        protected EntityBase()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
            Active = true;
        }
    }
}