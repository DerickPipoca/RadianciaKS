namespace RadianciaKS.Application.DTOs.Modifier
{
    public class ModifierOptionResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal AdditionalPrice { get; set; }
        public string? Description { get; set; }
    }
}