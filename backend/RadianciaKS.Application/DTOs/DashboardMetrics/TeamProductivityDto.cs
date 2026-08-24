using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.DTOs.DashboardMetrics
{
    public class TeamProductivityDto
    {
        public string EmployeeName { get; set; } = string.Empty;
        public EmployeeRole EmployeeRole { get; set; }

        public int CompletedTasks { get; set; }
    }
}