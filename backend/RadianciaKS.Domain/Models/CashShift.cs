using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Domain.Models
{
    public class CashShift : EntityBase
    {
        public Guid EmployeeOpenerId { get; set; }
        public Employee EmployeeOpener { get; set; } = null!;

        public Guid? EmployeeCloserId { get; set; }
        public Employee? EmployeeCloser { get; set; }

        //CreatedAt already exists in EntityBase
        public DateTime? ClosedAt { get; set; }

        public decimal InitialBalance { get; set; }
        public decimal? FinalCalculatedBalance { get; set; }
        public decimal? FinalReportedBalance { get; set; }

        public CashShiftStatus Status { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}