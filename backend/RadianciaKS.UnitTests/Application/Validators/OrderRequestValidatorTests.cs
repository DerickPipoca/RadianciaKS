using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.DTOs.Payment;
using RadianciaKS.Application.Validators.Order;

namespace RadianciaKS.UnitTests.Application.Validators
{
    public class OrderRequestValidatorTests
    {
        private readonly OrderRequestValidator _validator;

        public OrderRequestValidatorTests()
        {
            _validator = new OrderRequestValidator();
        }

        private static OrderRequestDto CreateValidDto(Action<OrderRequestDto>? configure = null)
        {
            var dto = new OrderRequestDto
            {
                TableNumber = "Mesa 12",
                Items =
                [
                    new OrderItemRequestDto
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = 2
                }
                ],
                Payments =
                [
                    new PaymentRequestDto
                {
                    Amount = 50.00m,
                    Method = PaymentMethod.CreditCard
                }
                ]
            };

            configure?.Invoke(dto);
            return dto;
        }

        [Fact]
        public void Should_Pass_When_OrderIsValid()
        {
            var dto = CreateValidDto();

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeTrue();
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Balcao")]
        [InlineData("Mesa 99")]
        public void Should_Pass_When_TableNumberIsValidOrEmpty(string? tableNumber)
        {
            var dto = CreateValidDto(x => x.TableNumber = tableNumber);

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.TableNumber);
        }

        [Fact]
        public void Should_Fail_When_TableNumberExceedsMaximumLength()
        {
            var dto = CreateValidDto(x => x.TableNumber = new string('M', 33));

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.TableNumber);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Should_Fail_When_OrderItemQuantityIsZeroOrNegative(int invalidQuantity)
        {
            var dto = CreateValidDto(x =>
            {
                x.Items =
                [
                    new OrderItemRequestDto
                {
                    ProductId = Guid.NewGuid(),
                    Quantity = invalidQuantity
                }
                ];
            });

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor("Items[0].Quantity");
        }

        [Fact]
        public void Should_Fail_When_OrderItemProductIdIsEmpty()
        {
            var dto = CreateValidDto(x =>
            {
                x.Items =
                [
                    new OrderItemRequestDto
                {
                    ProductId = Guid.Empty,
                    Quantity = 1
                }
                ];
            });

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor("Items[0].ProductId");
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-5.0)]
        public void Should_Fail_When_PaymentAmountIsZeroOrNegative(decimal invalidAmount)
        {
            var dto = CreateValidDto(x =>
            {
                x.Payments =
                [
                    new PaymentRequestDto
                {
                    Amount = invalidAmount,
                    Method = PaymentMethod.Cash
                }
                ];
            });

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor("Payments[0].Amount");
        }

        [Fact]
        public void Should_Fail_When_PaymentMethodIsDefaultOrEmpty()
        {
            var dto = CreateValidDto(x =>
            {
                x.Payments =
                [
                    new PaymentRequestDto
                {
                    Amount = 25.00m,
                    Method = default
                }
                ];
            });

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor("Payments[0].Method");
        }

        [Fact]
        public void Should_Fail_When_ItemsIsEmpty()
        {
            var dto = CreateValidDto(x => x.Items = []);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.Items);
        }

        [Fact]
        public void Should_Fail_When_ItemsIsNull()
        {
            var dto = CreateValidDto(x => x.Items = null!);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.Items);
        }
    }
}