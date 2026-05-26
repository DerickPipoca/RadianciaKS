using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.DTOs.Order
{
    public class OrderItemResponseDto
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; } = null!;
        public int Quantity { get; set; }
        public string? Notes { get; set; }
        public KdsStatus KdsStatus { get; set; }
    }
}