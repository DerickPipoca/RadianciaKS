using FluentValidation;
using RadianciaKS.Application.DTOs.Modifier;

namespace RadianciaKS.Application.Validators.ModifierOption
{
    public class ModifierOptionRequestValidator : AbstractValidator<ModifierOptionRequestDto>
    {
        public ModifierOptionRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome do produto é obrigatório.")
                .MaximumLength(100).WithMessage("O nome não pode exceder 100 caracteres.");

            RuleFor(x => x.AdditionalPrice)
                    .GreaterThanOrEqualTo(0).WithMessage("A preço do produto não pode ser negativo.");
        }
    }
}