using FluentValidation;
using RadianciaKS.Application.DTOs.Product;

namespace RadianciaKS.Application.Validators.Product
{
    public class ProductRequestValidator : AbstractValidator<ProductRequestDto>
    {
        public ProductRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome do produto é obrigatório.")
                .MaximumLength(100).WithMessage("O nome não pode exceder 100 caracteres.");

            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("A preço do produto não pode ser negativo.");

            RuleFor(x => x.CategoryId)
                .NotEmpty().WithMessage("A categoria deve ser válida.");
        }
    }
}