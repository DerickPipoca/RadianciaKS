namespace RadianciaKS.Domain.Models
{
    public class Category : EntityBase
    {
        public string Name { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public int? Priority { get; set; }
    }
}