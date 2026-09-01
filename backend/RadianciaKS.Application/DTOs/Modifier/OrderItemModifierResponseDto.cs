namespace RadianciaKS.Application.DTOs.Modifier
{
    public class OrderItemModifierResponseDto
    {
        public Guid Id { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal AdditionalPrice { get; set; }
        public decimal? OriginalAdditionalPrice { get; set; }
    }
}