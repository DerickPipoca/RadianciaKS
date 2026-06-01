namespace RadianciaKS.Application.DTOs.Modifier
{
    public class ModifierGroupRequestDto
    {
        public Guid ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinChoices { get; set; } = 0;
        public int MaxChoices { get; set; } = 1;
    }
}