using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using RadianciaKS.Application.DTOs.Category;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;
using RadianciaKS.Infrastructure.Context;
using RadianciaKS.IntegrationTests.Helpers;

namespace RadianciaKS.IntegrationTests.Controllers
{
    public class CategoryControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CategoryControllerIntegrationTests(CustomWebApplicationFactory factory)
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

        private async Task<Category> SeedCategoryAsync(string name, int priority = 1)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var category = new Category
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                Name = name,
                ImagePath = "https://cdn.radiancia.com/cat.png",
                Priority = priority,
                Active = true
            };

            context.Categories.Add(category);
            await context.SaveChangesAsync();
            return category;
        }

        [Fact]
        public async Task GetAllCategories_Should_Return401Unauthorized_When_NotAuthenticated()
        {
            var response = await _client.GetAsync("/api/category");

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateCategory_Should_Return403Forbidden_When_UserIsCashier()
        {
            var password = "Senha@123";
            var cashier = await SeedEmployeeAsync("10000000001", password, EmployeeRole.Cashier);
            await _client.AuthenticateAsAsync(cashier.CPF, password);

            var request = new CategoryRequestDto
            {
                Name = "Sobremesas",
                ImagePath = "https://cdn.radiancia.com/sobremesas.png",
                Priority = 2
            };

            var response = await _client.PostAsJsonAsync("/api/category", request);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateCategory_Should_Return201Created_When_UserIsAdmin()
        {
            var password = "Senha@123";
            var admin = await SeedEmployeeAsync("10000000002", password, EmployeeRole.Admin);
            await _client.AuthenticateAsAsync(admin.CPF, password);

            var request = new CategoryRequestDto
            {
                Name = "Lanches Especiais",
                ImagePath = "https://cdn.radiancia.com/lanches.png",
                Priority = 1
            };

            var response = await _client.PostAsJsonAsync("/api/category", request);

            response.StatusCode.ShouldBe(HttpStatusCode.Created);

            var content = await response.Content.ReadFromJsonAsync<CategoryResponseDto>();
            content.ShouldNotBeNull();
            content.Name.ShouldBe("Lanches Especiais");
            content.Priority.ShouldBe(1);
        }

        [Fact]
        public async Task GetAllCategories_Should_Return200OkWithList_When_Authenticated()
        {
            await SeedCategoryAsync("Bebidas", 1);
            await SeedCategoryAsync("Pizzas", 2);

            var password = "Senha@123";
            var cashier = await SeedEmployeeAsync("10000000003", password, EmployeeRole.Cashier);
            await _client.AuthenticateAsAsync(cashier.CPF, password);

            var response = await _client.GetAsync("/api/category");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var list = await response.Content.ReadFromJsonAsync<List<CategoryResponseDto>>();
            list.ShouldNotBeNull();
            list.Count.ShouldBe(2);
            list.ShouldContain(c => c.Name == "Bebidas");
            list.ShouldContain(c => c.Name == "Pizzas");
        }

        [Fact]
        public async Task GetCategoryById_Should_Return200Ok_When_CategoryExists()
        {
            var category = await SeedCategoryAsync("Porções", 3);

            var password = "Senha@123";
            var employee = await SeedEmployeeAsync("10000000004", password, EmployeeRole.Waiter);
            await _client.AuthenticateAsAsync(employee.CPF, password);

            var response = await _client.GetAsync($"/api/category/{category.Id}");

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<CategoryResponseDto>();
            content.ShouldNotBeNull();
            content.Id.ShouldBe(category.Id);
            content.Name.ShouldBe("Porções");
        }

        [Fact]
        public async Task UpdateCategory_Should_Return200Ok_When_UserIsManager()
        {
            var category = await SeedCategoryAsync("Bebidas Antigo", 1);

            var password = "Senha@123";
            var manager = await SeedEmployeeAsync("10000000005", password, EmployeeRole.Manager);
            await _client.AuthenticateAsAsync(manager.CPF, password);

            var request = new CategoryRequestDto
            {
                Name = "Bebidas Geladas",
                ImagePath = "https://cdn.radiancia.com/bebidas_atualizado.png",
                Priority = 5
            };

            var response = await _client.PutAsJsonAsync($"/api/category/{category.Id}", request);

            response.StatusCode.ShouldBe(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<CategoryResponseDto>();
            content.ShouldNotBeNull();
            content.Name.ShouldBe("Bebidas Geladas");
            content.Priority.ShouldBe(5);
        }

        [Fact]
        public async Task DeleteCategory_Should_Return204NoContent_When_UserIsAdmin()
        {
            var category = await SeedCategoryAsync("Excluir Categoria", 9);

            var password = "Senha@123";
            var admin = await SeedEmployeeAsync("10000000006", password, EmployeeRole.Admin);
            await _client.AuthenticateAsAsync(admin.CPF, password);

            var response = await _client.DeleteAsync($"/api/category/{category.Id}");

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }
    }
}