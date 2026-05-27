using FluentValidation;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.Validators.Payment;

namespace RadianciaKS.Application.Validators.Order
{
    public class OrderRequestValidator : AbstractValidator<OrderRequestDto>
    {
        public OrderRequestValidator()
        {
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Ao menos um item é necessário.");
            RuleFor(x => x.Payments)
                .NotEmpty().WithMessage("Ao menos um pagamento é necessário.");
            RuleForEach(x => x.Items)
                .SetValidator(new OrderItemRequestValidator());
            RuleForEach(x => x.Payments)
                .SetValidator(new PaymentRequestValidator());
        }
    }
}