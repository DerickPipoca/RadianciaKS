namespace RadianciaKS.Application.DTOs.Modifier
{
    public class OrderItemModifierResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal AdditionalPrice { get; set; }
    }
}