using RadianciaKS.Application.DTOs.Auth;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services.Auth;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.UnitTests.Application.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IApplicationDbContext> _contextMock;
        private readonly Mock<IPasswordService> _passwordServiceMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _contextMock = new Mock<IApplicationDbContext>();
            _passwordServiceMock = new Mock<IPasswordService>();
            _tokenServiceMock = new Mock<ITokenService>();

            _authService = new AuthService(
                _contextMock.Object,
                _passwordServiceMock.Object,
                _tokenServiceMock.Object
            );
        }

        private void SetupEmployeesDbSet(List<Employee> employees)
        {
            var mockDbSet = employees.BuildMockDbSet();
            _contextMock.Setup(c => c.Employees).Returns(mockDbSet.Object);
        }

        private static Employee CreateMockEmployee(Action<Employee>? configure = null)
        {
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                TenantId = Guid.NewGuid(),
                Name = "Carlos Atendente",
                CPF = "12345678900",
                Role = EmployeeRole.Cashier,
                Active = true,
                PasswordHash = "hash_senha_secreta"
            };

            configure?.Invoke(employee);
            return employee;
        }

        [Fact]
        public async Task Login_Should_ReturnLoginResponseDto_When_CredentialsAreValid()
        {
            var employee = CreateMockEmployee();
            SetupEmployeesDbSet([employee]);

            _passwordServiceMock
                .Setup(p => p.VerifyPassword("senha123", employee.PasswordHash))
                .Returns(true);

            _tokenServiceMock
                .Setup(t => t.GenerateToken(employee))
                .Returns("fake_jwt_token_xyz");

            var loginDto = new LoginRequestDto
            {
                CPF = employee.CPF,
                Password = "senha123"
            };

            var result = await _authService.Login(loginDto);

            result.ShouldNotBeNull();
            result.Token.ShouldBe("fake_jwt_token_xyz");
            result.Name.ShouldBe(employee.Name);
            result.Role.ShouldBe(employee.Role.ToString());

            _tokenServiceMock.Verify(t => t.GenerateToken(employee), Times.Once);
        }

        [Fact]
        public async Task Login_Should_ThrowUnauthorizedAccessException_When_EmployeeNotFound()
        {
            SetupEmployeesDbSet([]);

            var loginDto = new LoginRequestDto
            {
                CPF = "00000000000",
                Password = "senha"
            };

            var exception = await Should.ThrowAsync<UnauthorizedAccessException>(() =>
                _authService.Login(loginDto));

            exception.Message.ShouldContain("CPF ou senha incorretos.");
            _tokenServiceMock.Verify(t => t.GenerateToken(It.IsAny<Employee>()), Times.Never);
        }

        [Fact]
        public async Task Login_Should_ThrowUnauthorizedAccessException_When_EmployeeIsInactive()
        {
            var inactiveEmployee = CreateMockEmployee(e => e.Active = false);
            SetupEmployeesDbSet([inactiveEmployee]);

            var loginDto = new LoginRequestDto
            {
                CPF = inactiveEmployee.CPF,
                Password = "senha123"
            };

            var exception = await Should.ThrowAsync<UnauthorizedAccessException>(() =>
                _authService.Login(loginDto));

            exception.Message.ShouldContain("CPF ou senha incorretos.");
            _passwordServiceMock.Verify(p => p.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _tokenServiceMock.Verify(t => t.GenerateToken(It.IsAny<Employee>()), Times.Never);
        }

        [Fact]
        public async Task Login_Should_ThrowUnauthorizedAccessException_When_PasswordIsInvalid()
        {
            var employee = CreateMockEmployee();
            SetupEmployeesDbSet([employee]);

            _passwordServiceMock
                .Setup(p => p.VerifyPassword("senhaErrada", employee.PasswordHash))
                .Returns(false);

            var loginDto = new LoginRequestDto
            {
                CPF = employee.CPF,
                Password = "senhaErrada"
            };

            var exception = await Should.ThrowAsync<UnauthorizedAccessException>(() =>
                _authService.Login(loginDto));

            exception.Message.ShouldContain("CPF ou senha incorretos.");
            _tokenServiceMock.Verify(t => t.GenerateToken(It.IsAny<Employee>()), Times.Never);
        }
    }
}