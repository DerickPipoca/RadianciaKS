namespace RadianciaKS.Domain.Models
{
    public class ModifierGroup : EntityBase
    {
        public string Name { get; set; } = string.Empty;
        public int MinChoices { get; set; } = 0;
        public int MaxChoices { get; set; } = 1;

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public ICollection<ModifierOption> Options { get; set; } = new List<ModifierOption>();
    }
}