using FluentValidation;
using RadianciaKS.Application.DTOs.Order;

namespace RadianciaKS.Application.Validators.Order
{
    public class OrderItemRequestValidator : AbstractValidator<OrderItemRequestDto>
    {
        public OrderItemRequestValidator()
        {
            RuleFor(x => x.Quantity)
                .NotEmpty().WithMessage("A quantidade do item é obrigatória.")
                .GreaterThan(0).WithMessage("A quantidade do item deve ser acima de zero.");

            RuleFor(x => x.ProductId)
                .NotEmpty().WithMessage("O produto deve ser válido.");
        }

    }
}