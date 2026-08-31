namespace RadianciaKS.Application.DTOs.Promotion
{
    public class PromotionModifierRequestDto
    {
        public Guid ModifierOptionId { get; set; }
        public decimal OverridePrice { get; set; }
    }
}