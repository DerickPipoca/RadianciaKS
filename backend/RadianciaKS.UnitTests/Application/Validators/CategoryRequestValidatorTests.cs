using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RadianciaKS.Application.DTOs.Category;
using RadianciaKS.Application.Validators.Category;
using Xunit;

namespace RadianciaKS.UnitTests.Application.Validators
{
    public class CategoryRequestValidatorTests
    {
        private readonly CategoryRequestValidator _validator;

        public CategoryRequestValidatorTests()
        {
            _validator = new CategoryRequestValidator();
        }

        private static CategoryRequestDto CreateValidDto(Action<CategoryRequestDto>? configure = null)
        {
            var dto = new CategoryRequestDto
            {
                Name = "Bebidas",
                ImagePath = "https://storage.radiancia.com/images/drinks.png",
                Priority = 1
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
            var dto = CreateValidDto(x => x.Name = new string('C', 101));

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.Name);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public void Should_Fail_When_PriorityIsZeroOrNegative(int invalidPriority)
        {
            var dto = CreateValidDto(x => x.Priority = invalidPriority);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.Priority);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        public void Should_Pass_When_PriorityIsPositive(int validPriority)
        {
            var dto = CreateValidDto(x => x.Priority = validPriority);

            var result = _validator.TestValidate(dto);

            result.ShouldNotHaveValidationErrorFor(x => x.Priority);
        }

        [Fact]
        public void Should_Pass_When_PriorityAndImagePathAreNull()
        {
            var dto = CreateValidDto(x =>
            {
                x.Priority = null;
                x.ImagePath = null;
            });

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeTrue();
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}