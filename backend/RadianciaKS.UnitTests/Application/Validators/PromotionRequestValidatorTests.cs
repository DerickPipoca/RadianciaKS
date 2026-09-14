using RadianciaKS.Application.DTOs.Promotion;
using RadianciaKS.Application.Validators.Promotion;

namespace RadianciaKS.UnitTests.Application.Validators
{
    public class PromotionRequestValidatorTests
    {
        private readonly PromotionRequestValidator _validator;

        public PromotionRequestValidatorTests()
        {
            _validator = new PromotionRequestValidator();
        }

        private static PromotionRequestDto CreateValidDto(Action<PromotionRequestDto>? configure = null)
        {
            var dto = new PromotionRequestDto
            {
                Name = "Combo Almoço Executivo",
                Description = "Hambúrguer com refrigerante e batata frita inclusos.",
                BaseProductId = Guid.NewGuid(),
                PromotionalPrice = 29.90m,
                PromotionModifiers =
                [
                    new PromotionModifierRequestDto
                {
                    ModifierOptionId = Guid.NewGuid(),
                    OverridePrice = 5.00m
                }
                ]
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
        public void Should_Pass_When_PromotionalPriceIsNull_And_ModifiersEmpty()
        {
            var dto = CreateValidDto(x =>
            {
                x.PromotionalPrice = null;
                x.PromotionModifiers = [];
            });

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
            var dto = CreateValidDto(x => x.Name = new string('P', 101));

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Fact]
        public void Should_Fail_When_DescriptionExceedsMaximumLength()
        {
            var dto = CreateValidDto(x => x.Description = new string('D', 257));

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.Description);
        }

        [Fact]
        public void Should_Fail_When_BaseProductIdIsEmpty()
        {
            var dto = CreateValidDto(x => x.BaseProductId = Guid.Empty);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.BaseProductId);
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(-15.00)]
        public void Should_Fail_When_PromotionalPriceIsNegative(decimal negativePrice)
        {
            var dto = CreateValidDto(x => x.PromotionalPrice = negativePrice);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.PromotionalPrice);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(19.90)]
        public void Should_Pass_When_PromotionalPriceIsZeroOrPositive(decimal validPrice)
        {
            var dto = CreateValidDto(x => x.PromotionalPrice = validPrice);

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.PromotionalPrice);
        }

        [Fact]
        public void Should_Fail_When_PromotionModifierOptionIdIsEmpty()
        {
            var dto = CreateValidDto(x =>
            {
                x.PromotionModifiers =
                [
                    new PromotionModifierRequestDto
                {
                    ModifierOptionId = Guid.Empty,
                    OverridePrice = 2.00m
                }
                ];
            });

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor("PromotionModifiers[0].ModifierOptionId");
        }

        [Fact]
        public void Should_Fail_When_PromotionModifierOverridePriceIsNegative()
        {
            var dto = CreateValidDto(x =>
            {
                x.PromotionModifiers =
                [
                    new PromotionModifierRequestDto
                {
                    ModifierOptionId = Guid.NewGuid(),
                    OverridePrice = -1.00m
                }
                ];
            });

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor("PromotionModifiers[0].OverridePrice");
        }
    }
}