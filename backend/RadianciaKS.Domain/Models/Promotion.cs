namespace RadianciaKS.Domain.Models
{
    public class Promotion : EntityBase
    {
        public bool Running { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public Guid BaseProductId { get; set; }
        public Product BaseProduct { get; set; } = null!;

        public decimal? PromotionalPrice { get; set; }

        public virtual ICollection<PromotionModifier> PromotionModifiers { get; set; } = new List<PromotionModifier>();
    }
}