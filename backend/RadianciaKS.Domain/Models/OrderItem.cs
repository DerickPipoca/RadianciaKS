using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Domain.Models
{
    public class OrderItem : EntityBase
    {
        public int Quantity { get; set; }
        public string? Notes { get; set; }
        public decimal UnitPrice { get; set; }
        public KdsStatus KdsStatus { get; set; } = KdsStatus.Pending;

        public Guid OrderId { get; set; }
        public virtual Order Order { get; set; } = null!;

        public Guid ProductId { get; set; }
        public virtual Product Product { get; set; } = null!;

        public ICollection<OrderItemModifier> SelectedModifiers { get; set; } = new List<OrderItemModifier>();
    }
}