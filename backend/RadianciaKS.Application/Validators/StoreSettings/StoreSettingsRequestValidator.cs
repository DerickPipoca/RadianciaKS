using FluentValidation;
using RadianciaKS.Application.DTOs.StoreSettings;

namespace RadianciaKS.Application.Validators.StoreSettings
{
    public class StoreSettingsRequestValidator : AbstractValidator<StoreSettingsRequestDto>
    {
        public StoreSettingsRequestValidator()
        {
            RuleFor(x => x.StoreName)
            .NotEmpty().WithMessage("O nome da loja é obrigatória.")
            .MaximumLength(100).WithMessage("O nome da loja não pode exceder 100 caracteres.");

            RuleFor(x => x.CNPJ)
                .NotEmpty().WithMessage("O CNPJ da loja é obrigatório.")
                .Length(14).WithMessage("O CNPJ deve conter apenas 14 carácteres.");
        }
    }
}