using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RadianciaKS.Application.DTOs.StoreSettings;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;
using RadianciaKS.Infrastructure.Context;
using RadianciaKS.IntegrationTests.Helpers;

namespace RadianciaKS.IntegrationTests.Controllers
{
    public class StoreSettingsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public StoreSettingsControllerIntegrationTests(CustomWebApplicationFactory factory)
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

        private async Task<StoreSettings> SeedStoreSettingsAsync(string storeName = "Hamburgueria Radiancia")
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var settings = new StoreSettings
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                StoreName = storeName,
                CNPJ = "04.252.011/0001-10",
                Address = "Av. Brasil, 1500",
                Phone = "(44) 99999-8888",
                ReceiptFooter = "Obrigado pela preferência!",
                ServiceCharge = 10.0m,
                ChargeServiceByDefault = true,
                Active = true
            };

            context.StoreSettings.Add(settings);
            await context.SaveChangesAsync();
            return settings;
        }

        [Fact]
        public async Task GetStoreSettings_Should_Return200Ok_And_DefaultSettings_When_NoneExists()
        {
            var response = await _client.GetAsync("/api/storesettings");

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var settings = await response.Content.ReadFromJsonAsync<StoreSettingsResponseDto>();
            settings.ShouldNotBeNull();
            settings.StoreName.ShouldBe("Novo Restaurante");
        }

        [Fact]
        public async Task GetStoreSettings_Should_Return200Ok_When_SettingsExist()
        {
            await SeedStoreSettingsAsync("Restaurante Estrela da Noite");

            var response = await _client.GetAsync("/api/storesettings");

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var settings = await response.Content.ReadFromJsonAsync<StoreSettingsResponseDto>();
            settings.ShouldNotBeNull();
            settings.StoreName.ShouldBe("Restaurante Estrela da Noite");
        }

        [Fact]
        public async Task UpdateSettings_Should_Return401Unauthorized_When_NotAuthenticated()
        {
            var request = new StoreSettingsRequestDto
            {
                StoreName = "Nome Sem Autenticação",
                CNPJ = "04.252.011/0001-10",
                Address = "Rua Teste, 123",
                Phone = "(44) 98888-7777"
            };

            var response = await _client.PutAsJsonAsync("/api/storesettings", request);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateSettings_Should_Return403Forbidden_When_UserIsWaiter()
        {
            await SeedStoreSettingsAsync();

            var password = "Senha@123";
            var waiter = await SeedEmployeeAsync("85493217030", password, EmployeeRole.Waiter);
            await _client.AuthenticateAsAsync(waiter.CPF, password);

            var request = new StoreSettingsRequestDto
            {
                StoreName = "Tentativa de Atualização Garçom",
                CNPJ = "04.252.011/0001-10",
                Address = "Rua Teste, 123",
                Phone = "(44) 98888-7777"
            };

            var response = await _client.PutAsJsonAsync("/api/storesettings", request);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UpdateSettings_Should_Return200Ok_When_UserIsManager()
        {
            await SeedStoreSettingsAsync();

            var password = "Senha@123";
            var manager = await SeedEmployeeAsync("49082887398", password, EmployeeRole.Manager);
            await _client.AuthenticateAsAsync(manager.CPF, password);

            var updateRequest = new StoreSettingsRequestDto
            {
                StoreName = "Radiancia Grill & Bar",
                CNPJ = "04252011000110",
                Address = "Av. Central, 200",
                Phone = "(44) 99111-2233",
                ReceiptFooter = "Volte sempre!",
                ServiceCharge = 10.0m,
                ChargeServiceByDefault = true
            };

            var response = await _client.PutAsJsonAsync("/api/storesettings", updateRequest);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var updated = await response.Content.ReadFromJsonAsync<StoreSettingsResponseDto>();
            updated.ShouldNotBeNull();
            updated.StoreName.ShouldBe("Radiancia Grill & Bar");
            updated.Address.ShouldBe("Av. Central, 200");
        }

        [Fact]
        public async Task UploadImage_Should_Return403Forbidden_When_UserIsWaiter()
        {
            await SeedStoreSettingsAsync();

            var password = "Senha@123";
            var waiter = await SeedEmployeeAsync("52998224725", password, EmployeeRole.Waiter);
            await _client.AuthenticateAsAsync(waiter.CPF, password);

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
            content.Add(fileContent, "file", "logo.png");

            var response = await _client.PostAsync("/api/storesettings/upload-image?isBig=true", content);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UploadImage_Should_Return200Ok_When_UserIsAdmin_And_UploadSucceeds()
        {
            await SeedStoreSettingsAsync();

            var mockImageService = new Mock<IImageStorageService>();
            mockImageService
                .Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
                .ReturnsAsync("https://cdn.radianciaks.com/logos/logo_big.png");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockImageService.Object);
                });
            }).CreateClient();

            var password = "Senha@123";
            var admin = await SeedEmployeeAsync("63116114226", password, EmployeeRole.Admin);
            await client.AuthenticateAsAsync(admin.CPF, password);

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
            content.Add(fileContent, "file", "logo.png");

            var response = await client.PostAsync("/api/storesettings/upload-image?isBig=true", content);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);
            responseBody.ShouldContain("https://cdn.radianciaks.com/logos/logo_big.png");
        }
    }
}