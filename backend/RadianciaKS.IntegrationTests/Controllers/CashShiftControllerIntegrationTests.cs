
using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RadianciaKS.Application.DTOs.CashShift;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;
using RadianciaKS.Infrastructure.Context;
using RadianciaKS.IntegrationTests.Helpers;

namespace RadianciaKS.IntegrationTests.Controllers
{
    public class CashShiftControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CashShiftControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            await _factory.ResetDatabaseAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private async Task<Employee> SeedEmployeeAsync(string cpf, string rawPassword, EmployeeRole role = EmployeeRole.Cashier)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                Name = "Operador de Caixa",
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
        public async Task GetCurrentOpenShift_Should_Return204NoContent_When_NoShiftIsOpen()
        {
            var response = await _client.GetAsync("/api/cashshift/current");

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task OpenShift_Should_Return401Unauthorized_When_NotAuthenticated()
        {
            var request = new OpenCashShiftDto
            {
                InitialBalance = 150.00m
            };

            var response = await _client.PostAsJsonAsync("/api/cashshift/open", request);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task OpenShift_Should_Return200Ok_And_OpenShift_When_Authenticated()
        {
            var password = "Senha@123";
            var employee = await SeedEmployeeAsync("49082887398", password);
            await _client.AuthenticateAsAsync(employee.CPF, password);

            var request = new OpenCashShiftDto
            {
                InitialBalance = 200.00m
            };

            var response = await _client.PostAsJsonAsync("/api/cashshift/open", request);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<CashShiftResponseDto>();
            content.ShouldNotBeNull();
            content.Status.ShouldBe(CashShiftStatus.Open);
            content.InitialBalance.ShouldBe(200.00m);
        }

        [Fact]
        public async Task GetCurrentOpenShift_Should_Return200Ok_When_ThereIsOpenShift()
        {
            var password = "Senha@123";
            var employee = await SeedEmployeeAsync("49082887398", password);
            await _client.AuthenticateAsAsync(employee.CPF, password);

            var openDto = new OpenCashShiftDto { InitialBalance = 100.00m };
            await _client.PostAsJsonAsync("/api/cashshift/open", openDto);

            var response = await _client.GetAsync("/api/cashshift/current");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var currentShift = await response.Content.ReadFromJsonAsync<CashShiftResponseDto>();
            currentShift.ShouldNotBeNull();
            currentShift.Status.ShouldBe(CashShiftStatus.Open);
            currentShift.InitialBalance.ShouldBe(100.00m);
        }

        [Fact]
        public async Task CloseShift_Should_Return200Ok_And_CloseShift_When_Authenticated()
        {
            var password = "Senha@123";
            var employee = await SeedEmployeeAsync("49082887398", password);
            await _client.AuthenticateAsAsync(employee.CPF, password);

            await _client.PostAsJsonAsync("/api/cashshift/open", new OpenCashShiftDto
            {
                InitialBalance = 100.00m
            });

            var closeDto = new CloseCashShiftDto
            {
                FinalReportedBalance = 450.00m
            };

            var response = await _client.PostAsJsonAsync("/api/cashshift/close", closeDto);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var closedShift = await response.Content.ReadFromJsonAsync<CashShiftResponseDto>();
            closedShift.ShouldNotBeNull();
            closedShift.Status.ShouldBe(CashShiftStatus.Closed);
            closedShift.FinalReportedBalance.ShouldBe(450.00m);
        }

        [Fact]
        public async Task GetHistory_Should_Return200Ok_WithPagedData_When_Authenticated()
        {
            var password = "Senha@123";
            var employee = await SeedEmployeeAsync("49082887398", password);
            await _client.AuthenticateAsAsync(employee.CPF, password);

            await _client.PostAsJsonAsync("/api/cashshift/open", new OpenCashShiftDto { InitialBalance = 50.00m });
            await _client.PostAsJsonAsync("/api/cashshift/close", new CloseCashShiftDto { FinalReportedBalance = 150.00m });

            var response = await _client.GetAsync("/api/cashshift/history?pageNumber=1&pageSize=10");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }
}