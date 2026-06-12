namespace RadianciaKS.Application.DTOs.DashboardMetrics
{
    public class DashboardMetricsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal AverageTicket { get; set; }
        public int TotalOrders { get; set; }
        public List<TopSellingItemDto> TopSellingItems { get; set; } = new();
        public List<CashFlowDto> CashFlow { get; set; } = new();
        public List<SalesChartDto> SalesChart { get; set; } = new();
    }
}