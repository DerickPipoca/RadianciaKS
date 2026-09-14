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

            RuleFor(x => x.ServiceCharge)
                .GreaterThanOrEqualTo(0).WithMessage("A taxa de serviço deve ser maior que 0%");

            RuleFor(x => x.Phone)
                .MinimumLength(10).WithMessage("O telefone deve ter mais de 9 números.")
                .MaximumLength(16).WithMessage("O telefone deve ter menos de 15 números.");

            RuleFor(x => x.ReceiptFooter)
                .MaximumLength(256).WithMessage("A mensagem não deve passar de 256 caractéres.");
        }
    }
}