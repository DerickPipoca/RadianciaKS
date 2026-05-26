using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Domain.Models
{
    public class Order : EntityBase
    {
        public string? TableNumber { get; set; }
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Open;
        public decimal TotalAmount { get; set; }

        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public virtual ICollection<Payment> Payment { get; set; } = new List<Payment>();
    }
}