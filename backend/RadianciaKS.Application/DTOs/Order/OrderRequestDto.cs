using RadianciaKS.Application.DTOs.Payment;

namespace RadianciaKS.Application.DTOs.Order
{
    public class OrderRequestDto
    {
        public string? TableNumber { get; set; }
        public List<OrderItemRequestDto> Items { get; set; } = [];
        public List<PaymentRequestDto> Payments { get; set; } = [];
    }
}