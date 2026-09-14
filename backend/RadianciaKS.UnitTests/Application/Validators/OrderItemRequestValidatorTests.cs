using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.Validators.Order;

namespace RadianciaKS.UnitTests.Application.Validators
{
    public class OrderItemRequestValidatorTests
    {
        private readonly OrderItemRequestValidator _validator;

        public OrderItemRequestValidatorTests()
        {
            _validator = new OrderItemRequestValidator();
        }

        private static OrderItemRequestDto CreateValidDto(Action<OrderItemRequestDto>? configure = null)
        {
            var dto = new OrderItemRequestDto
            {
                Quantity = 2,
                ProductId = Guid.NewGuid(),
                Notes = "Sem cebola",
                PromotionId = Guid.NewGuid(),
                SelectedModifierIds = [Guid.NewGuid(), Guid.NewGuid()]
            };

            configure?.Invoke(dto);
            return dto;
        }

        [Fact]
        public void Should_Pass_When_AllFieldsAreValid()
        {
            var dto = CreateValidDto();

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeTrue();
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_Pass_When_OptionalFieldsAreNullOrEmpty()
        {
            var dto = CreateValidDto(x =>
            {
                x.Notes = null;
                x.PromotionId = null;
                x.SelectedModifierIds = [];
            });

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeTrue();
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void Should_Fail_When_QuantityIsZeroOrNegative(int invalidQuantity)
        {
            var dto = CreateValidDto(x => x.Quantity = invalidQuantity);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.Quantity);
        }

        [Fact]
        public void Should_Fail_When_ProductIdIsEmpty()
        {
            var dto = CreateValidDto(x => x.ProductId = Guid.Empty);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.ProductId);
        }
    }
}