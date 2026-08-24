using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.DTOs.DashboardMetrics
{
    public class DashboardMetricsDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal AverageTicket { get; set; }
        public int TotalOrders { get; set; }

        public decimal InitialBalance { get; set; }
        public decimal? FinalCalculatedBalance { get; set; }
        public decimal? FinalReportedBalance { get; set; }
        public CashShiftStatus ShiftStatus { get; set; }

        public string OpenedByName { get; set; } = string.Empty;
        public string? ClosedByName { get; set; }

        public int CanceledOrdersCount { get; set; }
        public decimal CanceledOrdersAmount { get; set; }

        public HistoryComparisonDto PreviousShiftComparison { get; set; } = new();

        public List<TopSellingItemDto> TopSellingItems { get; set; } = new();
        public List<CashFlowDto> CashFlow { get; set; } = new();
        public List<SalesChartDto> SalesChart { get; set; } = new();

        public List<TeamProductivityDto> WaiterProductivity { get; set; } = new();
    }
}