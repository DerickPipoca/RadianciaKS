using RadianciaKS.Application.DTOs.StoreSettings;
using RadianciaKS.Application.Validators.StoreSettings;

namespace RadianciaKS.UnitTests.Application.Validators
{
    public class StoreSettingsRequestValidatorTests
    {
        private readonly StoreSettingsRequestValidator _validator;

        public StoreSettingsRequestValidatorTests()
        {
            _validator = new StoreSettingsRequestValidator();
        }

        private static StoreSettingsRequestDto CreateValidDto(Action<StoreSettingsRequestDto>? configure = null)
        {
            var dto = new StoreSettingsRequestDto
            {
                StoreName = "Radiância Bar & Restaurante",
                CNPJ = "12345678000199",
                Address = "Av. Principal, 1000 - Centro",
                Phone = "44999999999",
                ReceiptFooter = "Obrigado pela preferência! Volte sempre.",
                ServiceCharge = 10.0m,
                ChargeServiceByDefault = true
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
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Should_Fail_When_StoreNameIsNullOrEmpty(string? storeName)
        {
            var dto = CreateValidDto(x => x.StoreName = storeName!);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.StoreName);
        }

        [Fact]
        public void Should_Fail_When_StoreNameExceedsMaximumLength()
        {
            var dto = CreateValidDto(x => x.StoreName = new string('A', 101));

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.StoreName);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Should_Fail_When_CNPJIsNullOrEmpty(string? cnpj)
        {
            var dto = CreateValidDto(x => x.CNPJ = cnpj!);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.CNPJ);
        }

        [Theory]
        [InlineData("12345678901")] // 11 dígitos (CPF)
        [InlineData("123456780001990")] // 15 dígitos
        [InlineData("12.345.678/0001-99")] // 18 dígitos com formatação
        public void Should_Fail_When_CNPJDoesNotHaveExactly14Digits(string invalidCnpj)
        {
            var dto = CreateValidDto(x => x.CNPJ = invalidCnpj);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.CNPJ);
        }

        [Fact]
        public void Should_Fail_When_ServiceChargeIsNegative()
        {
            var dto = CreateValidDto(x => x.ServiceCharge = -0.01m);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.ServiceCharge);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(10.0)]
        [InlineData(15.5)]
        public void Should_Pass_When_ServiceChargeIsZeroOrPositive(decimal validServiceCharge)
        {
            var dto = CreateValidDto(x => x.ServiceCharge = validServiceCharge);

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.ServiceCharge);
        }

        [Fact]
        public void Should_Fail_When_PhoneExceedsMaximumLength()
        {
            var dto = CreateValidDto(x => x.Phone = new string('9', 21));

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.Phone);
        }

        [Fact]
        public void Should_Fail_When_ReceiptFooterExceedsMaximumLength()
        {
            var dto = CreateValidDto(x => x.ReceiptFooter = new string('F', 257));

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.ReceiptFooter);
        }
    }
}