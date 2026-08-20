using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.DTOs.CashShift
{
    public class CashShiftResponseDto
    {
        public Guid Id { get; set; }
        public decimal InitialBalance { get; set; }
        public decimal? FinalCalculatedBalance { get; set; }
        public decimal? FinalReportedBalance { get; set; }
        public CashShiftStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public Guid EmployeeOpenerId { get; set; }
        public Guid? EmployeeCloserId { get; set; }
    }
}