namespace RadianciaKS.Application.DTOs.Product
{
    public class ProductRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImagePath { get; set; }
        public decimal Price { get; set; }
        public Guid CategoryId { get; set; }
    }
}