using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;
using RadianciaKS.Infrastructure.Context;
using RadianciaKS.IntegrationTests.Helpers;

namespace RadianciaKS.IntegrationTests.Controllers
{
    public class UploadControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public UploadControllerIntegrationTests(CustomWebApplicationFactory factory)
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

        private static MultipartFormDataContent CreateFileContent(string paramName = "file", string fileName = "foto.jpg", byte[]? bytes = null)
        {
            var content = new MultipartFormDataContent();
            var filePayload = bytes ?? new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
            var byteContent = new ByteArrayContent(filePayload);
            byteContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            content.Add(byteContent, paramName, fileName);
            return content;
        }

        [Fact]
        public async Task UploadImage_Should_Return401Unauthorized_When_NotAuthenticated()
        {
            using var content = CreateFileContent();

            var response = await _client.PostAsync("/api/upload", content);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UploadImage_Should_Return403Forbidden_When_UserIsWaiter()
        {
            var password = "Senha@123";
            var waiter = await SeedEmployeeAsync("85493217030", password, EmployeeRole.Waiter);
            await _client.AuthenticateAsAsync(waiter.CPF, password);

            using var content = CreateFileContent();

            var response = await _client.PostAsync("/api/upload", content);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UploadImage_Should_Return200Ok_When_UserIsManager_And_UploadSucceeds()
        {
            var expectedUrl = "https://cdn.radianciaks.com/images/produtos/prato-executivo.jpg";

            var mockStorage = new Mock<IImageStorageService>();
            mockStorage
                .Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>(), "images"))
                .ReturnsAsync(expectedUrl);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockStorage.Object);
                });
            }).CreateClient();

            var password = "Senha@123";
            var manager = await SeedEmployeeAsync("52998224725", password, EmployeeRole.Manager);
            await client.AuthenticateAsAsync(manager.CPF, password);

            using var content = CreateFileContent();

            var response = await client.PostAsync("/api/upload", content);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var result = await response.Content.ReadFromJsonAsync<UploadResponseDto>();
            result.ShouldNotBeNull();
            result.Url.ShouldBe(expectedUrl);
        }

        [Fact]
        public async Task UploadImage_Should_RespectCustomFolderName_When_UserIsAdmin()
        {
            var expectedUrl = "https://cdn.radianciaks.com/banners/promocao-natal.png";
            var customFolder = "banners";

            var mockStorage = new Mock<IImageStorageService>();
            mockStorage
                .Setup(s => s.UploadImageAsync(It.IsAny<IFormFile>(), customFolder))
                .ReturnsAsync(expectedUrl);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockStorage.Object);
                });
            }).CreateClient();

            var password = "Senha@123";
            var admin = await SeedEmployeeAsync("49082887398", password, EmployeeRole.Admin);
            await client.AuthenticateAsAsync(admin.CPF, password);

            using var content = CreateFileContent(fileName: "banner.png", bytes: new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            content.Add(new StringContent(customFolder), "folderName");

            var response = await client.PostAsync("/api/upload", content);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var result = await response.Content.ReadFromJsonAsync<UploadResponseDto>();
            result.ShouldNotBeNull();
            result.Url.ShouldBe(expectedUrl);
        }

        [Fact]
        public async Task UploadImage_Should_Return400BadRequest_When_FileIsEmpty()
        {
            var password = "Senha@123";
            var admin = await SeedEmployeeAsync("63116114226", password, EmployeeRole.Admin);
            await _client.AuthenticateAsAsync(admin.CPF, password);

            using var content = CreateFileContent(bytes: Array.Empty<byte>());

            var response = await _client.PostAsync("/api/upload", content);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, customMessage: responseBody);

            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            problem.ShouldNotBeNull();
            problem.Detail.ShouldBe("Nenhum arquivo válido foi enviado para upload.");
        }

        private class UploadResponseDto
        {
            public string Url { get; set; } = string.Empty;
        }
    }
}