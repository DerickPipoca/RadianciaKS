using FluentValidation;
using RadianciaKS.Application.DTOs.Employee;

namespace RadianciaKS.Application.Validators.Employee
{
    public class EmployeeUpdateValidator : AbstractValidator<EmployeeUpdateDto>
    {
        public EmployeeUpdateValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome do empregado é obrigatório.")
                .MaximumLength(100).WithMessage("O nome não pode exceder 100 caracteres.");

            RuleFor(x => x.Role)
                .IsInEnum().WithMessage("É necessário um cargo para o empregado.");

            RuleFor(x => x.Password)
                .MinimumLength(4).WithMessage("A senha deve ter ao menos 4 caractéres.")
                .MaximumLength(64).WithMessage("A senha deve ter menos de 64 caractéres.")
                .When(x => !string.IsNullOrEmpty(x.Password));
        }
    }
}