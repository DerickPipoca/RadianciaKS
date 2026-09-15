using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RadianciaKS.Application.DTOs;
using RadianciaKS.Application.DTOs.Product;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;
using RadianciaKS.Infrastructure.Context;
using RadianciaKS.IntegrationTests.Helpers;

namespace RadianciaKS.IntegrationTests.Controllers
{
    public class ProductControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ProductControllerIntegrationTests(CustomWebApplicationFactory factory)
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

        private async Task<Category> SeedCategoryAsync(string name = "Hamburgueres")
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var category = new Category
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                Name = name,
                Active = true
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync();
            return category;
        }

        private async Task<Product> SeedProductAsync(Guid categoryId, string name = "X-Salada Especial", decimal price = 28.00m)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var product = new Product
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                CategoryId = categoryId,
                Name = name,
                Price = price,
                Active = true
            };

            context.Products.Add(product);
            await context.SaveChangesAsync();
            return product;
        }

        [Fact]
        public async Task GetAllProducts_Should_Return401Unauthorized_When_NotAuthenticated()
        {
            var response = await _client.GetAsync("/api/product");

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetAllProducts_Should_Return200Ok_When_Authenticated()
        {
            var category = await SeedCategoryAsync();
            await SeedProductAsync(category.Id);

            var password = "Senha@123";
            var waiter = await SeedEmployeeAsync("85493217030", password, EmployeeRole.Waiter);
            await _client.AuthenticateAsAsync(waiter.CPF, password);

            var response = await _client.GetAsync("/api/product");

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var paged = await response.Content.ReadFromJsonAsync<PagedResponse<ProductResponseDto>>();
            paged.ShouldNotBeNull();
            paged.Data.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task CreateProduct_Should_Return403Forbidden_When_UserIsWaiter()
        {
            var category = await SeedCategoryAsync();
            var password = "Senha@123";
            var waiter = await SeedEmployeeAsync("52998224725", password, EmployeeRole.Waiter);
            await _client.AuthenticateAsAsync(waiter.CPF, password);

            var request = new ProductRequestDto
            {
                CategoryId = category.Id,
                Name = "Refrigerante Cola 350ml",
                Price = 7.50m
            };

            var response = await _client.PostAsJsonAsync("/api/product", request);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateProduct_Should_Return201Created_When_UserIsManager()
        {
            var category = await SeedCategoryAsync();
            var password = "Senha@123";
            var manager = await SeedEmployeeAsync("49082887398", password, EmployeeRole.Manager);
            await _client.AuthenticateAsAsync(manager.CPF, password);

            var request = new ProductRequestDto
            {
                CategoryId = category.Id,
                Name = "X-Bacon Smash",
                Price = 34.00m
            };

            var response = await _client.PostAsJsonAsync("/api/product", request);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.Created, customMessage: responseBody);

            var created = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
            created.ShouldNotBeNull();
            created.Name.ShouldBe("X-Bacon Smash");
            created.Price.ShouldBe(34.00m);
        }

        [Fact]
        public async Task GetProductById_Should_Return200Ok_When_ProductExists()
        {
            var category = await SeedCategoryAsync();
            var product = await SeedProductAsync(category.Id, "Suco Natural de Laranja", 12.00m);

            var password = "Senha@123";
            var cook = await SeedEmployeeAsync("63116114226", password, EmployeeRole.Kitchen);
            await _client.AuthenticateAsAsync(cook.CPF, password);

            var response = await _client.GetAsync($"/api/product/{product.Id}");

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var found = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
            found.ShouldNotBeNull();
            found.Id.ShouldBe(product.Id);
            found.Name.ShouldBe("Suco Natural de Laranja");
        }

        [Fact]
        public async Task UpdateProduct_Should_Return200Ok_When_UserIsAdmin()
        {
            var category = await SeedCategoryAsync();
            var product = await SeedProductAsync(category.Id, "Pizza Broto", 25.00m);

            var password = "Senha@123";
            var admin = await SeedEmployeeAsync("33344455566", password, EmployeeRole.Admin);
            await _client.AuthenticateAsAsync(admin.CPF, password);

            var updateRequest = new ProductRequestDto
            {
                CategoryId = category.Id,
                Name = "Pizza Broto Quatro Queijos",
                Price = 29.90m
            };

            var response = await _client.PutAsJsonAsync($"/api/product/{product.Id}", updateRequest);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var updated = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
            updated.ShouldNotBeNull();
            updated.Name.ShouldBe("Pizza Broto Quatro Queijos");
            updated.Price.ShouldBe(29.90m);
        }

        [Fact]
        public async Task DuplicateProduct_Should_Return200Ok_When_UserIsManager()
        {
            var category = await SeedCategoryAsync();
            var originalProduct = await SeedProductAsync(category.Id, "Chopp Artesanal IPA", 18.00m);

            var password = "Senha@123";
            var manager = await SeedEmployeeAsync("44455566677", password, EmployeeRole.Manager);
            await _client.AuthenticateAsAsync(manager.CPF, password);

            var response = await _client.PostAsync($"/api/product/{originalProduct.Id}/duplicate", null);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var duplicated = await response.Content.ReadFromJsonAsync<ProductResponseDto>();
            duplicated.ShouldNotBeNull();
            duplicated.Id.ShouldNotBe(originalProduct.Id);
            duplicated.Price.ShouldBe(originalProduct.Price);
        }

        [Fact]
        public async Task DeleteProduct_Should_Return204NoContent_When_UserIsAdmin()
        {
            var category = await SeedCategoryAsync();
            var product = await SeedProductAsync(category.Id, "Sobremesa Antiga", 10.00m);

            var password = "Senha@123";
            var admin = await SeedEmployeeAsync("66677788899", password, EmployeeRole.Admin);
            await _client.AuthenticateAsAsync(admin.CPF, password);

            var response = await _client.DeleteAsync($"/api/product/{product.Id}");

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task UploadImage_Should_Return200Ok_When_UserIsAdminAndUploadSucceeds()
        {
            var mockProductService = new Mock<IProductService>();
            mockProductService
                .Setup(p => p.UploadImage(It.IsAny<IFormFile>()))
                .ReturnsAsync("https://cdn.radianciaks.com/images/produto-teste.png");

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockProductService.Object);
                });
            }).CreateClient();

            var password = "Senha@123";
            var admin = await SeedEmployeeAsync("77788899900", password, EmployeeRole.Admin);
            await client.AuthenticateAsAsync(admin.CPF, password);

            using var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            content.Add(fileContent, "file", "hamburguer.jpg");

            var response = await client.PostAsync("/api/product/upload-image", content);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);
            responseBody.ShouldContain("https://cdn.radianciaks.com/images/produto-teste.png");
        }
    }
}