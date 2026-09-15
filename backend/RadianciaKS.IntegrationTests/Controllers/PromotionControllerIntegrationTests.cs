using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RadianciaKS.Application.DTOs;
using RadianciaKS.Application.DTOs.Promotion;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;
using RadianciaKS.Infrastructure.Context;
using RadianciaKS.IntegrationTests.Helpers;

namespace RadianciaKS.IntegrationTests.Controllers
{
    public class PromotionControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PromotionControllerIntegrationTests(CustomWebApplicationFactory factory)
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

        private async Task<Employee> SeedEmployeeAsync(string cpf, string rawPassword, EmployeeRole role = EmployeeRole.Manager)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

            var employee = new Employee
            {
                TenantId = TestTenantProvider.DefaultTenantId,
                Name = $"Funcionario {role}",
                CPF = cpf,
                PasswordHash = passwordService.HashPassword(rawPassword),
                Role = role,
            };

            context.Employees.Add(employee);
            await context.SaveChangesAsync();
            return employee;
        }

        private async Task<Product> SeedProductAsync(string name = "Pizza Calabresa", decimal price = 50.00m)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var category = new Category
            {
                TenantId = TestTenantProvider.DefaultTenantId,
                Name = "Pizzas Tradicionais",
            };

            var product = new Product
            {
                TenantId = TestTenantProvider.DefaultTenantId,
                CategoryId = category.Id,
                Name = name,
                Price = price,
            };

            context.Categories.Add(category);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            return product;
        }

        private async Task<Promotion> SeedPromotionAsync(Guid productId, string name = "Promoção Terça da Pizza", bool running = true)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var promotion = new Promotion
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                BaseProductId = productId,
                Name = name,
                PromotionalPrice = 39.90m,
                Running = running,
                Active = true
            };

            context.Promotions.Add(promotion);
            await context.SaveChangesAsync();

            return promotion;
        }

        [Fact]
        public async Task GetAllActivePromotions_Should_Return200Ok_And_ListActivePromotions()
        {
            var product = await SeedProductAsync();
            await SeedPromotionAsync(product.Id, "Promo Ativa", running: true);
            await SeedPromotionAsync(product.Id, "Promo Inativa", running: false);

            var password = "Senha@123";
            var employee = await SeedEmployeeAsync("85493217030", password, EmployeeRole.Waiter);
            await _client.AuthenticateAsAsync(employee.CPF, password);

            var response = await _client.GetAsync("/api/promotion/actives");

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var promotions = await response.Content.ReadFromJsonAsync<List<PromotionResponseDto>>();
            promotions.ShouldNotBeNull();
            promotions.ShouldNotBeEmpty();
            promotions.ShouldContain(p => p.Name == "Promo Ativa");
            promotions.ShouldNotContain(p => p.Name == "Promo Inativa");
        }

        [Fact]
        public async Task GetAllPromotions_Should_Return200Ok_When_Authenticated()
        {
            var product = await SeedProductAsync();
            await SeedPromotionAsync(product.Id, "Promoção Geral", running: true);

            var password = "Senha@123";
            var employee = await SeedEmployeeAsync("38221718670", password, EmployeeRole.Cashier);
            await _client.AuthenticateAsAsync(employee.CPF, password);

            var response = await _client.GetAsync("/api/promotion");

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var paged = await response.Content.ReadFromJsonAsync<PagedResponse<PromotionResponseDto>>();
            paged.ShouldNotBeNull();
            paged.Data.ShouldNotBeEmpty();
        }

        [Fact]
        public async Task GetPromotionById_Should_Return200Ok_When_PromotionExists()
        {
            var product = await SeedProductAsync();
            var promotion = await SeedPromotionAsync(product.Id, "Promoção Específica", running: true);

            var password = "Senha@123";
            var employee = await SeedEmployeeAsync("38221718670", password, EmployeeRole.Manager);
            await _client.AuthenticateAsAsync(employee.CPF, password);

            var response = await _client.GetAsync($"/api/promotion/{promotion.Id}");

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var found = await response.Content.ReadFromJsonAsync<PromotionResponseDto>();
            found.ShouldNotBeNull();
            found.Id.ShouldBe(promotion.Id);
            found.Name.ShouldBe("Promoção Específica");
        }

        [Fact]
        public async Task CreatePromotion_Should_Return201Created_When_DataIsValid()
        {
            var product = await SeedProductAsync("Burger Artesanal", 40.00m);

            var password = "Senha@123";
            var manager = await SeedEmployeeAsync("38221718670", password, EmployeeRole.Manager);
            await _client.AuthenticateAsAsync(manager.CPF, password);

            var request = new PromotionRequestDto
            {
                Name = "Festival do Burger",
                Description = "Hambúrguer com desconto especial",
                BaseProductId = product.Id,
                PromotionalPrice = 29.90m
            };

            var response = await _client.PostAsJsonAsync("/api/promotion", request);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.Created, customMessage: responseBody);

            var created = await response.Content.ReadFromJsonAsync<PromotionResponseDto>();
            created.ShouldNotBeNull();
            created.Name.ShouldBe("Festival do Burger");
            created.BaseProduct.ShouldNotBeNull();
            created.BaseProduct.Id.ShouldBe(product.Id);
        }

        [Fact]
        public async Task UpdatePromotion_Should_Return200Ok_When_PromotionExists()
        {
            var product = await SeedProductAsync();
            var promotion = await SeedPromotionAsync(product.Id, "Promoção Antiga", running: true);

            var password = "Senha@123";
            var manager = await SeedEmployeeAsync("38221718670", password, EmployeeRole.Manager);
            await _client.AuthenticateAsAsync(manager.CPF, password);

            var updateRequest = new PromotionRequestDto
            {
                Name = "Promoção Atualizada",
                Description = "Descrição atualizada",
                BaseProductId = product.Id,
                PromotionalPrice = 32.00m
            };

            var response = await _client.PutAsJsonAsync($"/api/promotion/{promotion.Id}", updateRequest);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var updated = await response.Content.ReadFromJsonAsync<PromotionResponseDto>();
            updated.ShouldNotBeNull();
            updated.Name.ShouldBe("Promoção Atualizada");
            updated.BaseProduct.ShouldNotBeNull();
            updated.BaseProduct.Id.ShouldBe(product.Id);
        }

        [Fact]
        public async Task ToggleRunningStatus_Should_Return200Ok_And_InvertStatus()
        {
            var product = await SeedProductAsync();
            var promotion = await SeedPromotionAsync(product.Id, "Promoção Toggle", running: false);

            var password = "Senha@123";
            var manager = await SeedEmployeeAsync("38221718670", password, EmployeeRole.Manager);
            await _client.AuthenticateAsAsync(manager.CPF, password);

            var response = await _client.PatchAsync($"/api/promotion/{promotion.Id}/toggle", null);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var result = await response.Content.ReadFromJsonAsync<bool>();
            result.ShouldBeTrue();
        }

        [Fact]
        public async Task DeletePromotion_Should_Return204NoContent_When_PromotionExists()
        {
            var product = await SeedProductAsync();
            var promotion = await SeedPromotionAsync(product.Id, "Promoção para Excluir", running: false);

            var password = "Senha@123";
            var admin = await SeedEmployeeAsync("38221718670", password, EmployeeRole.Admin);
            await _client.AuthenticateAsAsync(admin.CPF, password);

            var response = await _client.DeleteAsync($"/api/promotion/{promotion.Id}");

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }
    }
}