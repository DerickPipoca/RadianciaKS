namespace RadianciaKS.Application.DTOs.Promotion
{
    public class PromotionRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public Guid BaseProductId { get; set; }

        public decimal? PromotionalPrice { get; set; }

        public List<PromotionModifierRequestDto> PromotionModifiers { get; set; } = new List<PromotionModifierRequestDto>();
    }
}