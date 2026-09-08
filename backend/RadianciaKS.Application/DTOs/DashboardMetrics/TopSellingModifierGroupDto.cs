namespace RadianciaKS.Application.DTOs.DashboardMetrics
{
    public class TopSellingModifierGroupDto
    {
        public string GroupName { get; set; } = string.Empty;
        public List<TopSellingModifierDto> Modifiers { get; set; } = new();
    }
}