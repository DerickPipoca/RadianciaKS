namespace RadianciaKS.Application.DTOs.DashboardMetrics
{
    public class CashFlowDto
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}