namespace RadianciaKS.Application.DTOs.Category
{
    public class CategoryRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public int? Priority { get; set; }
    }
}