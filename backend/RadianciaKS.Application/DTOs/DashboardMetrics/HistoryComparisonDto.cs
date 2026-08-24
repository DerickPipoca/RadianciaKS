namespace RadianciaKS.Application.DTOs.DashboardMetrics
{
    public class HistoryComparisonDto
    {
        public bool HasPreviousShift { get; set; }
        public decimal RevenuePercentage { get; set; }
        public decimal OrdersPercentage { get; set; }
    }
}