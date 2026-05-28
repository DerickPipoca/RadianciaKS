namespace RadianciaKS.Application.DTOs.Category
{
    public class CategoryResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? Priority { get; set; }
        public string? ImagePath { get; set; }
    }
}