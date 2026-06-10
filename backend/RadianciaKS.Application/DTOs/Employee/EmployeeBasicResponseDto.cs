using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.DTOs.Employee
{
    public class EmployeeBasicResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public EmployeeRole Role { get; set; }
    }
}