using RadianciaKS.Application.DTOs.Modifier;
using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.DTOs.Order
{
    public class OrderItemResponseDto
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public Guid CategoryId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public KdsStatus KdsStatus { get; set; }

        public List<OrderItemModifierResponseDto> SelectedModifiers { get; set; } = new();
    }
}