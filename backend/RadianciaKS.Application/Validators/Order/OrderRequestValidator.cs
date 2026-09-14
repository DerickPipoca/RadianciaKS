using FluentValidation;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.Validators.Payment;

namespace RadianciaKS.Application.Validators.Order
{
    public class OrderRequestValidator : AbstractValidator<OrderRequestDto>
    {
        public OrderRequestValidator()
        {
            RuleFor(x => x.TableNumber)
                .MaximumLength(32).WithMessage("O nome da mesa não pode exceder 32 caracteres.");
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("O pedido deve conter ao menos um item.");
            RuleForEach(x => x.Items)
                .SetValidator(new OrderItemRequestValidator());
            RuleForEach(x => x.Payments)
                .SetValidator(new PaymentRequestValidator());
        }
    }
}