using FluentValidation;
using RadianciaKS.Application.DTOs.Payment;

namespace RadianciaKS.Application.Validators.Payment
{
    public class PaymentRequestValidator : AbstractValidator<PaymentRequestDto>
    {
        public PaymentRequestValidator()
        {
            RuleFor(x => x.Amount)
                .GreaterThan(0).WithMessage("O valor deve ser acima de 0,00.");

            RuleFor(x => x.Method)
                .NotEmpty().WithMessage("O método de pagamento deve ser válido.");
        }
    }
}