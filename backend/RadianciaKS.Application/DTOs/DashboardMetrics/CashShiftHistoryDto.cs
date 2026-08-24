using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.DTOs.DashboardMetrics
{
    public class CashShiftHistoryDto
    {
        public Guid CashShiftId { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public CashShiftStatus Status { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}