using RadianciaKS.Application.DTOs.Modifier;

namespace RadianciaKS.Application.DTOs.Product
{
    public class ProductResponseDto
    {
        public Guid Id { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public decimal Price { get; set; }

        public bool IsPromotional { get; set; }
        public decimal? PromotionalPrice { get; set; }

        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        public List<ModifierGroupResponseDto> ModifierGroups { get; set; } = new();
    }
}