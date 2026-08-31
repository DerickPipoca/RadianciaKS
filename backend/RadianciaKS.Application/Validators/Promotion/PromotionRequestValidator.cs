using FluentValidation;
using RadianciaKS.Application.DTOs.Promotion;

namespace RadianciaKS.Application.Validators.Promotion
{
    public class PromotionRequestValidator : AbstractValidator<PromotionRequestDto>
    {
        public PromotionRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome da promoção é obrigatório.")
                .MaximumLength(100).WithMessage("O nome não pode exceder 100 caracteres.");

            RuleFor(x => x.Description)
                .MaximumLength(256).WithMessage("A descrição não pode exceder 256 caracteres.");

            RuleFor(x => x.BaseProductId)
                .NotEmpty().WithMessage("O produto base da promoção é obrigatório.");

            RuleFor(x => x.PromotionalPrice)
                .GreaterThanOrEqualTo(0).When(x => x.PromotionalPrice.HasValue)
                .WithMessage("O preço promocional não pode ser negativo.");

            RuleForEach(x => x.PromotionModifiers).SetValidator(new PromotionModifierRequestValidator());
        }
    }

    public class PromotionModifierRequestValidator : AbstractValidator<PromotionModifierRequestDto>
    {
        public PromotionModifierRequestValidator()
        {
            RuleFor(x => x.ModifierOptionId)
                .NotEmpty().WithMessage("A opção do modificador é obrigatória.");

            RuleFor(x => x.OverridePrice)
                .GreaterThanOrEqualTo(0).WithMessage("O preço do modificador promocional não pode ser negativo.");
        }
    }
}