using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RadianciaKS.Application.DTOs.Employee;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;
using RadianciaKS.Infrastructure.Context;
using RadianciaKS.IntegrationTests.Helpers;

namespace RadianciaKS.IntegrationTests.Controllers
{
    public class EmployeeControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public EmployeeControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            await _factory.ResetDatabaseAsync();
            _client.DefaultRequestHeaders.Authorization = null;
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private async Task<Employee> SeedEmployeeAsync(string cpf, string rawPassword, EmployeeRole role)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                Name = $"Funcionario {role}",
                CPF = cpf,
                PasswordHash = passwordService.HashPassword(rawPassword),
                Role = role,
                Active = true
            };

            context.Employees.Add(employee);
            await context.SaveChangesAsync();
            return employee;
        }

        [Fact]
        public async Task GetAllEmployees_Should_Return401Unauthorized_When_NotAuthenticated()
        {
            var response = await _client.GetAsync("/api/employee");

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAllEmployees_Should_Return403Forbidden_When_UserIsCashier()
        {
            var password = "Senha@123";
            var cashier = await SeedEmployeeAsync("11111111101", password, EmployeeRole.Cashier);
            await _client.AuthenticateAsAsync(cashier.CPF, password);

            var response = await _client.GetAsync("/api/employee");

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetAllEmployees_Should_Return200Ok_When_UserIsAdmin()
        {
            var password = "Senha@123";
            var admin = await SeedEmployeeAsync("11111111102", password, EmployeeRole.Admin);
            await _client.AuthenticateAsAsync(admin.CPF, password);

            var response = await _client.GetAsync("/api/employee");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var employees = await response.Content.ReadFromJsonAsync<List<EmployeeResponseDto>>();
            employees.ShouldNotBeNull();
            employees.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task CreateEmployee_Should_Return201Created_When_UserIsManager()
        {
            var password = "Senha@123";
            var manager = await SeedEmployeeAsync("49082887398", password, EmployeeRole.Manager);
            await _client.AuthenticateAsAsync(manager.CPF, password);

            var request = new EmployeeRequestDto
            {
                Name = "Novo Garçom",
                CPF = "63116114226",
                Password = "SenhaGarcom@123",
                Role = EmployeeRole.Waiter
            };

            var response = await _client.PostAsJsonAsync("/api/employee", request);

            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            var created = await response.Content.ReadFromJsonAsync<EmployeeResponseDto>();
            created.ShouldNotBeNull();
            created.Name.ShouldBe("Novo Garçom");
            created.CPF.ShouldBe("63116114226");
            created.Role.ShouldBe(EmployeeRole.Waiter);
        }

        [Fact]
        public async Task GetEmployeeById_Should_Return200Ok_When_EmployeeExists()
        {
            var password = "Senha@123";
            var admin = await SeedEmployeeAsync("11111111104", password, EmployeeRole.Admin);
            var targetEmployee = await SeedEmployeeAsync("33344455566", password, EmployeeRole.Waiter);

            await _client.AuthenticateAsAsync(admin.CPF, password);

            var response = await _client.GetAsync($"/api/employee/{targetEmployee.Id}");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<EmployeeResponseDto>();
            content.ShouldNotBeNull();
            content.Id.ShouldBe(targetEmployee.Id);
            content.Name.ShouldBe(targetEmployee.Name);
        }

        [Fact]
        public async Task GetEmployeeDetailsById_Should_Return200Ok_FromAdminRoute()
        {
            var password = "Senha@123";
            var admin = await SeedEmployeeAsync("11111111105", password, EmployeeRole.Admin);
            var targetEmployee = await SeedEmployeeAsync("44455566677", password, EmployeeRole.Kitchen);

            await _client.AuthenticateAsAsync(admin.CPF, password);

            var response = await _client.GetAsync($"/api/employee/admin/{targetEmployee.Id}");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<EmployeeResponseDto>();
            content.ShouldNotBeNull();
            content.Id.ShouldBe(targetEmployee.Id);
        }

        [Fact]
        public async Task UpdateEmployee_Should_Return200Ok_When_UserIsAdmin()
        {
            var password = "Senha@123";
            var admin = await SeedEmployeeAsync("49082887398", password, EmployeeRole.Admin);
            var targetEmployee = await SeedEmployeeAsync("63116114226", password, EmployeeRole.Cashier);

            await _client.AuthenticateAsAsync(admin.CPF, password);

            var updateDto = new EmployeeUpdateDto
            {
                Name = "Nome Atualizado",
                CPF = targetEmployee.CPF,
                Role = EmployeeRole.Manager
            };

            var response = await _client.PutAsJsonAsync($"/api/employee/{targetEmployee.Id}", updateDto);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var updated = await response.Content.ReadFromJsonAsync<EmployeeResponseDto>();
            updated.ShouldNotBeNull();
            updated.Name.ShouldBe("Nome Atualizado");
            updated.Role.ShouldBe(EmployeeRole.Manager);
        }

        [Fact]
        public async Task DeleteEmployee_Should_Return204NoContent_When_UserIsManager()
        {
            var password = "Senha@123";
            var manager = await SeedEmployeeAsync("11111111107", password, EmployeeRole.Manager);
            var targetEmployee = await SeedEmployeeAsync("66677788899", password, EmployeeRole.Waiter);

            await _client.AuthenticateAsAsync(manager.CPF, password);

            var response = await _client.DeleteAsync($"/api/employee/{targetEmployee.Id}");

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }
    }
}