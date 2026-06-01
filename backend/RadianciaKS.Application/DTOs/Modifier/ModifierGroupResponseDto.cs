namespace RadianciaKS.Application.DTOs.Modifier
{
    public class ModifierGroupResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinChoices { get; set; }
        public int MaxChoices { get; set; }
        public List<ModifierOptionResponseDto> Options { get; set; } = new();
    }
}