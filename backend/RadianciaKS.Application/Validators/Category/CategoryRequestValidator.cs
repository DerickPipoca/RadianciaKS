using FluentValidation;
using RadianciaKS.Application.DTOs.Category;

namespace RadianciaKS.Application.Validators.Category
{
    public class CategoryRequestValidator : AbstractValidator<CategoryRequestDto>
    {
        public CategoryRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome da categoria é obrigatório.")
                .MaximumLength(100).WithMessage("O nome não pode exceder 100 caracteres.");

            RuleFor(x => x.Priority)
                .GreaterThan(0).When(x => x.Priority.HasValue)
                .WithMessage("A prioridade não pode ser zero ou negativa.");
        }
    }
}