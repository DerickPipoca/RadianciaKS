namespace RadianciaKS.Domain.Models
{
    public class ModifierOption : EntityBase
    {
        public string Name { get; set; } = string.Empty;
        public decimal AdditionalPrice { get; set; } = 0m;

        public Guid ModifierGroupId { get; set; }
        public ModifierGroup ModifierGroup { get; set; } = null!;
    }
}