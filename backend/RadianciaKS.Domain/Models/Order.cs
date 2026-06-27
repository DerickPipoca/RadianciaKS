using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Domain.Models
{
    public class Order : EntityBase
    {
        public string? TableNumber { get; set; }
        public OrderStatus OrderStatus { get; set; } = OrderStatus.Open;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public decimal TotalAmount { get; set; }
        public string? ReceiptUrl { get; set; }

        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

        public Guid? PaidById { get; set; }
        public Employee? PaidBy { get; set; }

        public Guid EmployeeId { get; set; }
        public virtual Employee Employee { get; set; } = null!;
    }
}