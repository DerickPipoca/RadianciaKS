using RadianciaKS.Application.DTOs.Payment;
using RadianciaKS.Application.Validators.Payment;

namespace RadianciaKS.UnitTests.Application.Validators
{
    public class PaymentRequestValidatorTests
    {
        private readonly PaymentRequestValidator _validator;

        public PaymentRequestValidatorTests()
        {
            _validator = new PaymentRequestValidator();
        }

        private static PaymentRequestDto CreateValidDto(Action<PaymentRequestDto>? configure = null)
        {
            var dto = new PaymentRequestDto
            {
                Amount = 50.00m,
                Method = PaymentMethod.CreditCard
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

        [Theory]
        [InlineData(0.0)]
        [InlineData(-0.01)]
        [InlineData(-50.00)]
        public void Should_Fail_When_AmountIsZeroOrNegative(decimal invalidAmount)
        {
            var dto = CreateValidDto(x => x.Amount = invalidAmount);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.Amount);
        }

        [Theory]
        [InlineData(0.01)]
        [InlineData(1.00)]
        [InlineData(150.75)]
        public void Should_Pass_When_AmountIsGreaterThanZero(decimal validAmount)
        {
            var dto = CreateValidDto(x => x.Amount = validAmount);

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.Amount);
        }

        [Fact]
        public void Should_Fail_When_PaymentMethodIsDefault()
        {
            var dto = CreateValidDto(x => x.Method = default);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.Method);
        }
    }
}