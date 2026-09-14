using RadianciaKS.Application.DTOs.Modifier;
using RadianciaKS.Application.Validators.ModifierOption;

namespace RadianciaKS.UnitTests.Application.Validators
{
    public class ModifierOptionRequestValidatorTests
    {
        private readonly ModifierOptionRequestValidator _validator;

        public ModifierOptionRequestValidatorTests()
        {
            _validator = new ModifierOptionRequestValidator();
        }

        private static ModifierOptionRequestDto CreateValidDto(Action<ModifierOptionRequestDto>? configure = null)
        {
            var dto = new ModifierOptionRequestDto
            {
                Name = "Bacon Extra",
                ImagePath = "https://storage.radiancia.com/images/bacon.png",
                AdditionalPrice = 4.50m,
                Description = "Fatias crocantes de bacon artesanal"
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
        public void Should_Fail_When_NameIsNullOrEmpty(string? name)
        {
            var dto = CreateValidDto(x => x.Name = name!);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Fail_When_NameExceedsMaximumLength()
        {
            var dto = CreateValidDto(x => x.Name = new string('A', 101));

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(-10.0)]
        public void Should_Fail_When_AdditionalPriceIsNegative(decimal negativePrice)
        {
            var dto = CreateValidDto(x => x.AdditionalPrice = negativePrice);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.AdditionalPrice);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.50)]
        [InlineData(12.0)]
        public void Should_Pass_When_AdditionalPriceIsZeroOrPositive(decimal validPrice)
        {
            var dto = CreateValidDto(x => x.AdditionalPrice = validPrice);

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.AdditionalPrice);
        }

        [Fact]
        public void Should_Pass_When_OptionalDescriptionIsNull()
        {
            var dto = CreateValidDto(x => x.Description = null);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeTrue();
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}