using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Domain.Models
{
    public class Employee : EntityBase
    {
        public string Name { get; set; } = string.Empty;

        public DateOnly? Birthday { get; set; }
        public string CPF { get; set; } = string.Empty;

        public EmployeeRole Role { get; set; }

        public string PasswordHash { get; set; } = string.Empty;

        public void Update(string? name, DateOnly? birthday, string? cpf, EmployeeRole? role, string? passwordHash)
        {
            if (name != null)
                Name = name;

            if (birthday != null)
                Birthday = birthday;

            if (cpf != null)
                CPF = cpf;

            if (role != null)
                Role = (EmployeeRole)role;

            if (passwordHash != null)
                PasswordHash = passwordHash;
        }
    }
}