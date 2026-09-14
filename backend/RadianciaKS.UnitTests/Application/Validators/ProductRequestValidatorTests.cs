using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadianciaKS.Application.DTOs.Product;
using RadianciaKS.Application.Validators.Product;
using Xunit;

namespace RadianciaKS.UnitTests.Application.Validators
{
    public class ProductRequestValidatorTests
    {
        private readonly ProductRequestValidator _validator;

        public ProductRequestValidatorTests()
        {
            _validator = new ProductRequestValidator();
        }

        private static ProductRequestDto CreateValidDto(Action<ProductRequestDto>? configure = null)
        {
            var dto = new ProductRequestDto
            {
                Name = "Hambúrguer Artesanal",
                Description = "Pão brioche, blend 180g, queijo cheddar e bacon.",
                ImagePath = "https://storage.radiancia.com/images/burger.png",
                Price = 32.50m,
                CategoryId = Guid.NewGuid()
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
        public void Should_Fail_When_PriceIsNegative(decimal negativePrice)
        {
            var dto = CreateValidDto(x => x.Price = negativePrice);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.Price);
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(0.01)]
        [InlineData(45.90)]
        public void Should_Pass_When_PriceIsZeroOrPositive(decimal validPrice)
        {
            var dto = CreateValidDto(x => x.Price = validPrice);

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.Price);
        }

        [Fact]
        public void Should_Fail_When_CategoryIdIsEmpty()
        {
            var dto = CreateValidDto(x => x.CategoryId = Guid.Empty);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.CategoryId);
        }

        [Fact]
        public void Should_Pass_When_OptionalFieldsAreNull()
        {
            var dto = CreateValidDto(x =>
            {
                x.Description = null;
                x.ImagePath = null;
            });

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeTrue();
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}