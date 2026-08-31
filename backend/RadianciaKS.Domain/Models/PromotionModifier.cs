namespace RadianciaKS.Domain.Models
{
    public class PromotionModifier : EntityBase
    {
        public Guid PromotionId { get; set; }
        public virtual Promotion Promotion { get; set; } = null!;

        public Guid ModifierOptionId { get; set; }
        public virtual ModifierOption ModifierOption { get; set; } = null!;

        public decimal OverridePrice { get; set; }
    }
}