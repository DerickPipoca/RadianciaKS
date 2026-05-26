using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Domain.Models
{
    public class Payment : EntityBase
    {
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }

        public Guid OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;
    }
}