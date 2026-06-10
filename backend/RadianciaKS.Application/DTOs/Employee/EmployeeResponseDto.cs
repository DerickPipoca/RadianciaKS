using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.DTOs.Employee
{
    public class EmployeeResponseDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateOnly? Birthday { get; set; }
        public string? CPF { get; set; } = string.Empty;
        public EmployeeRole Role { get; set; }
    }
}