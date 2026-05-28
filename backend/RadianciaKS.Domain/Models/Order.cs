using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Domain.Models
{
    public class Order : EntityBase
    {
        public string? TableNumber { get; set; }
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Open;
        public decimal TotalAmount { get; set; }
        public string? ReceiptUrl { get; set; }

        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}