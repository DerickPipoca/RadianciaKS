namespace RadianciaKS.Application.DTOs.Promotion
{
    public class PromotionRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        // O produto que vai sofrer a alteração de preço
        public Guid BaseProductId { get; set; }

        // Opcional: Se a promoção der desconto no produto principal
        public decimal? PromotionalPrice { get; set; }

        // Lista com os modificadores que terão desconto
        public List<PromotionModifierRequestDto> PromotionModifiers { get; set; } = new List<PromotionModifierRequestDto>();
    }
}