using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RadianciaKS.Application.DTOs.Modifier;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;
using RadianciaKS.Infrastructure.Context;
using RadianciaKS.IntegrationTests.Helpers;

namespace RadianciaKS.IntegrationTests.Controllers
{
    public class ModifierControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public ModifierControllerIntegrationTests(CustomWebApplicationFactory factory)
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

        private async Task<Product> SeedProductAsync()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var category = new Category
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                Name = "Lanches",
                Active = true
            };

            var product = new Product
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                CategoryId = category.Id,
                Name = "X-Burguer Especial",
                Price = 28.00m,
                Active = true
            };

            context.Categories.Add(category);
            context.Products.Add(product);
            await context.SaveChangesAsync();

            return product;
        }

        private async Task<(ModifierGroup group, ModifierOption option)> SeedModifierGroupWithOptionsAsync(Guid productId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var group = new ModifierGroup
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                ProductId = productId,
                Name = "Ponto da Carne",
                MinChoices = 1,
                MaxChoices = 1,
                Active = true
            };

            var option = new ModifierOption
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                ModifierGroupId = group.Id,
                Name = "Ao Ponto",
                AdditionalPrice = 0.00m,
                Active = true
            };

            group.Options.Add(option);

            context.ModifierGroups.Add(group);
            context.ModifierOptions.Add(option);
            await context.SaveChangesAsync();

            return (group, option);
        }

        [Fact]
        public async Task CreateGroup_Should_Return401Unauthorized_When_NotAuthenticated()
        {
            var request = new ModifierGroupRequestDto
            {
                ProductId = Guid.NewGuid(),
                Name = "Adicionais"
            };

            var response = await _client.PostAsJsonAsync("/api/modifier/groups", request);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateGroup_Should_Return403Forbidden_When_UserIsCashier()
        {
            var password = "Senha@123";
            var cashier = await SeedEmployeeAsync("11111111101", password, EmployeeRole.Cashier);
            await _client.AuthenticateAsAsync(cashier.CPF, password);

            var product = await SeedProductAsync();
            var request = new ModifierGroupRequestDto
            {
                ProductId = product.Id,
                Name = "Molhos Extras"
            };

            var response = await _client.PostAsJsonAsync("/api/modifier/groups", request);

            response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateGroup_Should_Return201Created_When_UserIsManager()
        {
            var password = "Senha@123";
            var manager = await SeedEmployeeAsync("49082887398", password, EmployeeRole.Manager);
            await _client.AuthenticateAsAsync(manager.CPF, password);

            var product = await SeedProductAsync();
            var request = new ModifierGroupRequestDto
            {
                ProductId = product.Id,
                Name = "Adicionais Premium",
                MinChoices = 0,
                MaxChoices = 3
            };

            var response = await _client.PostAsJsonAsync("/api/modifier/groups", request);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.Created, customMessage: responseBody);

            var content = await response.Content.ReadFromJsonAsync<ModifierGroupResponseDto>();
            content.ShouldNotBeNull();
            content.Name.ShouldBe("Adicionais Premium");
        }

        [Fact]
        public async Task AddOptionToGroup_Should_Return200Ok_When_UserIsAdmin()
        {
            var password = "Senha@123";
            var admin = await SeedEmployeeAsync("85493217030", password, EmployeeRole.Admin);
            await _client.AuthenticateAsAsync(admin.CPF, password);

            var product = await SeedProductAsync();
            var (group, _) = await SeedModifierGroupWithOptionsAsync(product.Id);

            var request = new ModifierOptionRequestDto
            {
                Name = "Bacon Crocante",
                AdditionalPrice = 6.50m
            };

            var response = await _client.PostAsJsonAsync($"/api/modifier/groups/{group.Id}/options", request);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var content = await response.Content.ReadFromJsonAsync<ModifierOptionResponseDto>();
            content.ShouldNotBeNull();
            content.Name.ShouldBe("Bacon Crocante");
            content.AdditionalPrice.ShouldBe(6.50m);
        }

        [Fact]
        public async Task GetGroupsByProduct_Should_Return200Ok_When_Authenticated()
        {
            var password = "Senha@123";
            var waiter = await SeedEmployeeAsync("52998224725", password, EmployeeRole.Waiter);
            await _client.AuthenticateAsAsync(waiter.CPF, password);

            var product = await SeedProductAsync();
            await SeedModifierGroupWithOptionsAsync(product.Id);

            var response = await _client.GetAsync($"/api/modifier/products/{product.Id}");

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var list = await response.Content.ReadFromJsonAsync<List<ModifierGroupResponseDto>>();
            list.ShouldNotBeNull();
            list.ShouldNotBeEmpty();
            list.ShouldContain(g => g.Name == "Ponto da Carne");
        }

        [Fact]
        public async Task UpdateGroup_Should_Return200Ok_When_Authenticated()
        {
            var password = "Senha@123";
            var manager = await SeedEmployeeAsync("63116114226", password, EmployeeRole.Manager);
            await _client.AuthenticateAsAsync(manager.CPF, password);

            var product = await SeedProductAsync();
            var (group, _) = await SeedModifierGroupWithOptionsAsync(product.Id);

            var updateRequest = new ModifierGroupRequestDto
            {
                ProductId = product.Id,
                Name = "Ponto da Carne Atualizado",
                MinChoices = 1,
                MaxChoices = 2
            };

            var response = await _client.PutAsJsonAsync($"/api/modifier/groups/{group.Id}", updateRequest);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var updated = await response.Content.ReadFromJsonAsync<ModifierGroupResponseDto>();
            updated.ShouldNotBeNull();
            updated.Name.ShouldBe("Ponto da Carne Atualizado");
        }

        [Fact]
        public async Task UpdateOption_Should_Return200Ok_When_Authenticated()
        {
            var password = "Senha@123";
            var manager = await SeedEmployeeAsync("33344455566", password, EmployeeRole.Manager);
            await _client.AuthenticateAsAsync(manager.CPF, password);

            var product = await SeedProductAsync();
            var (_, option) = await SeedModifierGroupWithOptionsAsync(product.Id);

            var updateRequest = new ModifierOptionRequestDto
            {
                Name = "Bem Passado",
                AdditionalPrice = 2.00m
            };

            var response = await _client.PutAsJsonAsync($"/api/modifier/options/{option.Id}", updateRequest);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var updated = await response.Content.ReadFromJsonAsync<ModifierOptionResponseDto>();
            updated.ShouldNotBeNull();
            updated.Name.ShouldBe("Bem Passado");
            updated.AdditionalPrice.ShouldBe(2.00m);
        }

        [Fact]
        public async Task DeleteOption_Should_Return204NoContent_When_UserIsAdmin()
        {
            var password = "Senha@123";
            var admin = await SeedEmployeeAsync("44455566677", password, EmployeeRole.Admin);
            await _client.AuthenticateAsAsync(admin.CPF, password);

            var product = await SeedProductAsync();
            var (_, option) = await SeedModifierGroupWithOptionsAsync(product.Id);

            var response = await _client.DeleteAsync($"/api/modifier/options/{option.Id}");

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task DeleteGroup_Should_Return204NoContent_When_UserIsManager()
        {
            var password = "Senha@123";
            var manager = await SeedEmployeeAsync("66677788899", password, EmployeeRole.Manager);
            await _client.AuthenticateAsAsync(manager.CPF, password);

            var product = await SeedProductAsync();
            var (group, _) = await SeedModifierGroupWithOptionsAsync(product.Id);

            var response = await _client.DeleteAsync($"/api/modifier/groups/{group.Id}");

            response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }
    }
}