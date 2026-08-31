using RadianciaKS.Application.DTOs.Product;

namespace RadianciaKS.Application.DTOs.Promotion
{
    public class PromotionResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public ProductResponseDto BaseProduct { get; set; } = null!;
    }
}