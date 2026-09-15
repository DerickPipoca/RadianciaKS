using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using RadianciaKS.Application.DTOs.Auth;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;
using RadianciaKS.Infrastructure.Context;

namespace RadianciaKS.IntegrationTests.Controllers
{
    public class AuthControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AuthControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        public async Task InitializeAsync()
        {
            await _factory.ResetDatabaseAsync();
        }

        private async Task<Employee> SeedEmployeeAsync(string cpf, string rawPassword, bool active = true)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

            var employee = new Employee
            {
                TenantId = TestTenantProvider.DefaultTenantId,
                Name = "Operador Integração",
                CPF = cpf,
                PasswordHash = passwordService.HashPassword(rawPassword),
                Role = EmployeeRole.Cashier,
                Active = active
            };

            context.Employees.Add(employee);
            await context.SaveChangesAsync();
            return employee;
        }

        [Fact]
        public async Task Login_Should_Return200OkWithToken_When_CredentialsAreValid()
        {
            var rawPassword = "SenhaValida@123";
            var employee = await SeedEmployeeAsync("12345678901", rawPassword);

            var request = new LoginRequestDto
            {
                CPF = employee.CPF,
                Password = rawPassword
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", request);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            content.ShouldNotBeNull();
            content.Name.ShouldBe(employee.Name);
            content.Role.ShouldBe(employee.Role.ToString());
            content.Token.ShouldNotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Login_Should_Return401Unauthorized_When_PasswordIsIncorrect()
        {
            var employee = await SeedEmployeeAsync("12345678902", "SenhaCorreta@123");

            var request = new LoginRequestDto
            {
                CPF = employee.CPF,
                Password = "SenhaErrada!999"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", request);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            problemDetails.ShouldNotBeNull();
            problemDetails.Detail.ShouldBe("CPF ou senha incorretos.");
        }

        [Fact]
        public async Task Login_Should_Return401Unauthorized_When_EmployeeIsInactive()
        {
            var employee = await SeedEmployeeAsync("12345678903", "Senha@123", active: false);

            var request = new LoginRequestDto
            {
                CPF = employee.CPF,
                Password = "Senha@123"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", request);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }
    }
}