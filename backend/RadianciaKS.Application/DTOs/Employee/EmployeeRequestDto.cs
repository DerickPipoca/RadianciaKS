using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.DTOs.Employee
{
    public class EmployeeRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public DateOnly? Birthday { get; set; }
        public string? CPF { get; set; } = string.Empty;
        public EmployeeRole Role { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}