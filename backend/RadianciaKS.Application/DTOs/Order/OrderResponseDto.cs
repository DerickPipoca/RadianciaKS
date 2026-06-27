using RadianciaKS.Application.DTOs.Payment;
using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.DTOs.Order
{
    public class OrderResponseDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? TableNumber { get; set; }
        public string? ReceiptUrl { get; set; }
        public OrderStatus OrderStatus { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public Decimal TotalAmount { get; set; }
        public Decimal ChangeAmount { get; set; }

        public Guid EmployeeId { get; set; }

        public string CreatedByName { get; set; } = string.Empty;
        public string? PaidByName { get; set; }

        public List<OrderItemResponseDto> Items { get; set; } = [];
        public List<PaymentResponseDto> Payments { get; set; } = [];
    }
}