using RadianciaKS.Application.DTOs.Employee;
using RadianciaKS.Application.Validators.Employee;

namespace RadianciaKS.UnitTests.Application.Validators
{
    public class EmployeeRequestValidatorTests
    {
        private readonly EmployeeRequestValidator _validator;

        public EmployeeRequestValidatorTests()
        {
            _validator = new EmployeeRequestValidator();
        }

        private static EmployeeRequestDto CreateValidDto(Action<EmployeeRequestDto>? configure = null)
        {
            var dto = new EmployeeRequestDto
            {
                Name = "Carlos Eduardo Santos",
                CPF = "88999958426",
                Password = "SenhaSegura@123",
                Role = EmployeeRole.Cashier
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
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Should_Fail_When_CPFIsNullOrEmpty(string? cpf)
        {
            var dto = CreateValidDto(x => x.CPF = cpf!);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.CPF);
        }

        [Theory]
        [InlineData("12345678901")]     // 11 dígitos com cálculo inválido
        [InlineData("00000000000")]     // Dígitos repetidos
        [InlineData("1114447773")]      // Incompleto (10 dígitos)
        [InlineData("111.444.777-35")]  // Formatado com máscara
        public void Should_Fail_When_CPFIsInvalidAccordingToAlgorithm(string invalidCpf)
        {
            // Arrange
            var dto = CreateValidDto(x => x.CPF = invalidCpf);

            // Act
            var result = _validator.TestValidate(dto);

            // Assert
            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.CPF);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public void Should_Fail_When_PasswordIsNullOrEmpty(string? password)
        {
            var dto = CreateValidDto(x => x.Password = password!);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Theory]
        [InlineData("12")]
        [InlineData("12345123451234512345123451234512345")]
        public void Should_Fail_When_PasswordIsBelowMinimumLength(string shortPassword)
        {
            var dto = CreateValidDto(x => x.Password = shortPassword);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.Password);
        }

        [Fact]
        public void Should_Fail_When_RoleIsInvalid()
        {
            var dto = CreateValidDto(x => x.Role = (EmployeeRole)999);

            var result = _validator.TestValidate(dto);

            result.IsValid.ShouldBeFalse();
            result.ShouldHaveValidationErrorFor(x => x.Role);
        }
    }
}