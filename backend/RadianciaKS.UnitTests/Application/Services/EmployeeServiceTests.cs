using FluentValidation;
using RadianciaKS.Application.DTOs.Employee;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.UnitTests.Application.Services
{
    public class EmployeeServiceTests
    {
        private readonly Mock<IApplicationDbContext> _contextMock;
        private readonly Mock<IPasswordService> _passwordServiceMock;
        private readonly Mock<IValidator<EmployeeRequestDto>> _validatorMock;
        private readonly Mock<IValidator<EmployeeUpdateDto>> _updateValidatorMock;
        private readonly Mock<IUserProvider> _userProviderMock;
        private readonly EmployeeService _employeeService;

        public EmployeeServiceTests()
        {
            _contextMock = new Mock<IApplicationDbContext>();
            _passwordServiceMock = new Mock<IPasswordService>();
            _validatorMock = new Mock<IValidator<EmployeeRequestDto>>();
            _updateValidatorMock = new Mock<IValidator<EmployeeUpdateDto>>();
            _userProviderMock = new Mock<IUserProvider>();

            // Validações configuradas como sucesso por padrão
            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _updateValidatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _employeeService = new EmployeeService(
                _contextMock.Object,
                _passwordServiceMock.Object,
                _validatorMock.Object,
                _updateValidatorMock.Object,
                _userProviderMock.Object
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
                Name = "João da Silva",
                CPF = "12345678901",
                Birthday = new DateOnly(1995, 5, 20),
                Role = EmployeeRole.Waiter,
                Active = true,
                PasswordHash = "hashed_default_secret"
            };

            configure?.Invoke(employee);
            return employee;
        }

        [Fact]
        public async Task GetEmployeeById_Should_ReturnDto_When_EmployeeExists()
        {
            var employee = CreateMockEmployee();
            SetupEmployeesDbSet([employee]);
            _contextMock.Setup(c => c.Employees.FindAsync(employee.Id)).ReturnsAsync(employee);

            var result = await _employeeService.GetEmployeeById(employee.Id);

            result.ShouldNotBeNull();
            result.Id.ShouldBe(employee.Id);
            result.Name.ShouldBe(employee.Name);
            result.Role.ShouldBe(EmployeeRole.Waiter);
        }

        [Fact]
        public async Task GetEmployeeById_Should_ThrowArgumentException_When_EmployeeNotFound()
        {
            var id = Guid.NewGuid();
            SetupEmployeesDbSet([]);
            _contextMock.Setup(c => c.Employees.FindAsync(id)).ReturnsAsync((Employee?)null);

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _employeeService.GetEmployeeById(id));

            exception.Message.ShouldContain("O funcionário informado não existe.");
        }

        [Fact]
        public async Task GetBasicInformationEmployeeById_Should_ReturnBasicDto_When_Found()
        {
            var employee = CreateMockEmployee(e => e.Name = "Maria Atendente");
            SetupEmployeesDbSet([employee]);
            _contextMock.Setup(c => c.Employees.FindAsync(employee.Id)).ReturnsAsync(employee);

            var result = await _employeeService.GetBasicInformationEmployeeById(employee.Id);

            result.ShouldNotBeNull();
            result.Id.ShouldBe(employee.Id);
            result.Name.ShouldBe("Maria Atendente");
        }

        [Fact]
        public async Task GetAllEmployees_Should_ReturnAllEmployeesMapped()
        {
            var emp1 = CreateMockEmployee(e => e.Name = "Func 1");
            var emp2 = CreateMockEmployee(e => e.Name = "Func 2");

            SetupEmployeesDbSet([emp1, emp2]);

            var result = (await _employeeService.GetAllEmployees()).ToList();

            result.Count.ShouldBe(2);
            result.ShouldContain(e => e.Name == "Func 1");
            result.ShouldContain(e => e.Name == "Func 2");
        }

        [Fact]
        public async Task CreateEmployee_Should_HashPassword_And_Save_When_Valid()
        {
            _userProviderMock.Setup(u => u.GetUserRole()).Returns(EmployeeRole.Admin.ToString());
            _passwordServiceMock.Setup(p => p.HashPassword("senhaForte123")).Returns("hash_senhaForte123");

            SetupEmployeesDbSet([]);

            var dto = new EmployeeRequestDto
            {
                Name = "Novo Garçom",
                CPF = "11122233344",
                Birthday = new DateOnly(2000, 1, 1),
                Role = EmployeeRole.Waiter,
                Password = "senhaForte123"
            };

            var result = await _employeeService.CreateEmployee(dto);

            result.ShouldNotBeNull();
            result.Name.ShouldBe("Novo Garçom");
            result.Role.ShouldBe(EmployeeRole.Waiter);

            _passwordServiceMock.Verify(p => p.HashPassword("senhaForte123"), Times.Once);
            _contextMock.Verify(c => c.Employees.AddAsync(It.Is<Employee>(e =>
                e.Name == "Novo Garçom" &&
                e.PasswordHash == "hash_senhaForte123" &&
                e.Role == EmployeeRole.Waiter), default), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task CreateEmployee_Should_ThrowArgumentException_When_ManagerTriesToCreateAdmin()
        {
            _userProviderMock.Setup(u => u.GetUserRole()).Returns(EmployeeRole.Manager.ToString());

            var dto = new EmployeeRequestDto
            {
                Name = "Tentativa Admin",
                Role = EmployeeRole.Admin,
                Password = "123"
            };

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _employeeService.CreateEmployee(dto));

            exception.Message.ShouldContain("Apenas Administradores podem conceder permissão de Administrador.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task CreateEmployee_Should_ThrowArgumentException_When_CurrentUserHasNoValidRole()
        {
            _userProviderMock.Setup(u => u.GetUserRole()).Returns(string.Empty);

            var dto = new EmployeeRequestDto { Role = EmployeeRole.Waiter };

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _employeeService.CreateEmployee(dto));

            exception.Message.ShouldContain("Usuário não autenticado ou com cargo inválido.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task UpdateEmployee_Should_UpdateFields_And_HashNewPassword_When_Valid()
        {
            var currentUserId = Guid.NewGuid();
            var targetEmployee = CreateMockEmployee(e =>
            {
                e.Id = Guid.NewGuid();
                e.Role = EmployeeRole.Waiter;
            });

            _userProviderMock.Setup(u => u.GetUserId()).Returns(currentUserId);
            _userProviderMock.Setup(u => u.GetUserRole()).Returns(EmployeeRole.Admin.ToString());
            _passwordServiceMock.Setup(p => p.HashPassword("novaSenha456")).Returns("hash_novaSenha456");

            SetupEmployeesDbSet([targetEmployee]);
            _contextMock.Setup(c => c.Employees.FindAsync(targetEmployee.Id)).ReturnsAsync(targetEmployee);

            var updateDto = new EmployeeUpdateDto
            {
                Name = "João Atualizado",
                CPF = "99988877766",
                Birthday = new DateOnly(1996, 6, 15),
                Role = EmployeeRole.Kitchen,
                Password = "novaSenha456"
            };

            var result = await _employeeService.UpdateEmployee(targetEmployee.Id, updateDto);

            result.ShouldNotBeNull();
            targetEmployee.Name.ShouldBe("João Atualizado");
            targetEmployee.Role.ShouldBe(EmployeeRole.Kitchen);
            targetEmployee.PasswordHash.ShouldBe("hash_novaSenha456");

            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UpdateEmployee_Should_ThrowArgumentException_When_UserTriesToUpdateThemselves()
        {
            var loggedUserId = Guid.NewGuid();
            var targetEmployee = CreateMockEmployee(e => e.Id = loggedUserId);

            _userProviderMock.Setup(u => u.GetUserId()).Returns(loggedUserId);
            _userProviderMock.Setup(u => u.GetUserRole()).Returns(EmployeeRole.Admin.ToString());

            SetupEmployeesDbSet([targetEmployee]);
            _contextMock.Setup(c => c.Employees.FindAsync(loggedUserId)).ReturnsAsync(targetEmployee);

            var updateDto = new EmployeeUpdateDto { Role = EmployeeRole.Admin };

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _employeeService.UpdateEmployee(loggedUserId, updateDto));

            exception.Message.ShouldContain("Não é possível alterar a conta logada pela mesma.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task UpdateEmployee_Should_ThrowArgumentException_When_ManagerTriesToUpdateAdmin()
        {
            var managerId = Guid.NewGuid();
            var adminTarget = CreateMockEmployee(e =>
            {
                e.Id = Guid.NewGuid();
                e.Role = EmployeeRole.Admin;
            });

            _userProviderMock.Setup(u => u.GetUserId()).Returns(managerId);
            _userProviderMock.Setup(u => u.GetUserRole()).Returns(EmployeeRole.Manager.ToString());

            SetupEmployeesDbSet([adminTarget]);
            _contextMock.Setup(c => c.Employees.FindAsync(adminTarget.Id)).ReturnsAsync(adminTarget);

            var updateDto = new EmployeeUpdateDto { Role = EmployeeRole.Admin };

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _employeeService.UpdateEmployee(adminTarget.Id, updateDto));

            exception.Message.ShouldContain("Gerentes não podem alterar ou excluir Administradores.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task UpdateEmployee_Should_ThrowArgumentException_When_ManagerTriesToUpdateAnotherManager()
        {
            var currentManagerId = Guid.NewGuid();
            var anotherManager = CreateMockEmployee(e =>
            {
                e.Id = Guid.NewGuid();
                e.Role = EmployeeRole.Manager;
            });

            _userProviderMock.Setup(u => u.GetUserId()).Returns(currentManagerId);
            _userProviderMock.Setup(u => u.GetUserRole()).Returns(EmployeeRole.Manager.ToString());

            SetupEmployeesDbSet([anotherManager]);
            _contextMock.Setup(c => c.Employees.FindAsync(anotherManager.Id)).ReturnsAsync(anotherManager);

            var updateDto = new EmployeeUpdateDto { Role = EmployeeRole.Manager };

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _employeeService.UpdateEmployee(anotherManager.Id, updateDto));

            exception.Message.ShouldContain("Gerentes não podem alterar ou excluir outros Gerentes.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task DeleteEmployee_Should_SetSoftDelete_When_Valid()
        {
            var currentAdminId = Guid.NewGuid();
            var targetEmployee = CreateMockEmployee(e =>
            {
                e.Id = Guid.NewGuid();
                e.Role = EmployeeRole.Waiter;
                e.Active = true;
            });

            _userProviderMock.Setup(u => u.GetUserId()).Returns(currentAdminId);
            _userProviderMock.Setup(u => u.GetUserRole()).Returns(EmployeeRole.Admin.ToString());

            SetupEmployeesDbSet([targetEmployee]);
            _contextMock.Setup(c => c.Employees.FindAsync(targetEmployee.Id)).ReturnsAsync(targetEmployee);

            await _employeeService.DeleteEmployee(targetEmployee.Id);

            targetEmployee.Active.ShouldBeFalse();
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task DeleteEmployee_Should_ThrowArgumentException_When_UserTriesToDeleteThemselves()
        {
            var loggedUserId = Guid.NewGuid();
            var targetEmployee = CreateMockEmployee(e => e.Id = loggedUserId);

            _userProviderMock.Setup(u => u.GetUserId()).Returns(loggedUserId);
            _userProviderMock.Setup(u => u.GetUserRole()).Returns(EmployeeRole.Admin.ToString());

            SetupEmployeesDbSet([targetEmployee]);
            _contextMock.Setup(c => c.Employees.FindAsync(loggedUserId)).ReturnsAsync(targetEmployee);

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _employeeService.DeleteEmployee(loggedUserId));

            exception.Message.ShouldContain("Não é possível alterar a conta logada pela mesma.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task DeleteEmployee_Should_ThrowArgumentException_When_ManagerTriesToDeleteAdmin()
        {
            var managerId = Guid.NewGuid();
            var adminTarget = CreateMockEmployee(e =>
            {
                e.Id = Guid.NewGuid();
                e.Role = EmployeeRole.Admin;
            });

            _userProviderMock.Setup(u => u.GetUserId()).Returns(managerId);
            _userProviderMock.Setup(u => u.GetUserRole()).Returns(EmployeeRole.Manager.ToString());

            SetupEmployeesDbSet([adminTarget]);
            _contextMock.Setup(c => c.Employees.FindAsync(adminTarget.Id)).ReturnsAsync(adminTarget);

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _employeeService.DeleteEmployee(adminTarget.Id));

            exception.Message.ShouldContain("Gerentes não podem alterar ou excluir Administradores.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task DeleteEmployee_Should_ThrowArgumentException_When_NotFound()
        {
            var id = Guid.NewGuid();
            SetupEmployeesDbSet([]);
            _contextMock.Setup(c => c.Employees.FindAsync(id)).ReturnsAsync((Employee?)null);

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _employeeService.DeleteEmployee(id));

            exception.Message.ShouldContain("O funcionário informado não existe.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

    }
}