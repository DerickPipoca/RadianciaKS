namespace RadianciaKS.Application.DTOs.Order
{
    public class OrderItemRequestDto
    {
        public int Quantity { get; set; }
        public string? Notes { get; set; }
        public Guid ProductId { get; set; }
    }
}