using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;
using RadianciaKS.Infrastructure.Context;
using RadianciaKS.IntegrationTests.Helpers;

namespace RadianciaKS.IntegrationTests.Controllers
{
    public class KdsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public KdsControllerIntegrationTests(CustomWebApplicationFactory factory)
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

        private async Task<Employee> SeedEmployeeAsync(string cpf, string rawPassword, EmployeeRole role = EmployeeRole.Kitchen)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                Name = "Cozinheiro KDS",
                CPF = cpf,
                PasswordHash = passwordService.HashPassword(rawPassword),
                Role = role,
            };

            context.Employees.Add(employee);
            await context.SaveChangesAsync();
            return employee;
        }

        private async Task<(Order order, OrderItem item)> SeedOrderWithItemAsync(KdsStatus initialStatus = KdsStatus.Pending)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

            // 1. Cria o funcionário responsável pelo pedido para satisfazer o .Include(o => o.Employee)
            var employee = new Employee
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                Name = "Garçom do Pedido",
                CPF = "49082887398",
                PasswordHash = passwordService.HashPassword("Senha@123"),
                Role = EmployeeRole.Waiter,
                Active = true
            };

            var category = new Category
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                Name = "Cozinha",
                Active = true
            };

            var product = new Product
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                CategoryId = category.Id,
                Name = "Hambúrguer Artesanal",
                Price = 35.00m,
                Active = true
            };

            var order = new Order
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                TableNumber = "101",
                OrderStatus = OrderStatus.Open,
                EmployeeId = employee.Id,
                Employee = employee,
                Active = true
            };

            var orderItem = new OrderItem
            {
                Id = Guid.NewGuid(),
                TenantId = TestTenantProvider.DefaultTenantId,
                OrderId = order.Id,
                Order = order,
                ProductId = product.Id,
                Product = product,
                Quantity = 1,
                UnitPrice = 35.00m,
                KdsStatus = initialStatus,
                Active = true
            };

            order.Items.Add(orderItem);

            context.Employees.Add(employee);
            context.Categories.Add(category);
            context.Products.Add(product);
            context.Orders.Add(order);
            context.OrderItems.Add(orderItem);

            await context.SaveChangesAsync();

            return (order, orderItem);
        }

        [Fact]
        public async Task GetPendingItems_Should_Return401Unauthorized_When_NotAuthenticated()
        {
            var response = await _client.GetAsync("/api/kds/pending");

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetPendingItems_Should_Return200Ok_When_Authenticated()
        {
            await SeedOrderWithItemAsync(KdsStatus.Pending);

            var password = "Senha@123";
            var cook = await SeedEmployeeAsync("85493217030", password, EmployeeRole.Kitchen);
            await _client.AuthenticateAsAsync(cook.CPF, password);

            var response = await _client.GetAsync("/api/kds/pending");

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);
        }

        [Fact]
        public async Task UpdateItemStatus_Should_Return401Unauthorized_When_NotAuthenticated()
        {
            var fakeOrderId = Guid.NewGuid();
            var fakeItemId = Guid.NewGuid();

            var response = await _client.PutAsJsonAsync($"/api/kds/{fakeOrderId}/items/{fakeItemId}/status", KdsStatus.Preparing);

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task UpdateItemStatus_Should_Return200Ok_When_AuthenticatedAndItemExists()
        {
            var (order, item) = await SeedOrderWithItemAsync(KdsStatus.Pending);

            var password = "Senha@123";
            var cook = await SeedEmployeeAsync("52998224725", password, EmployeeRole.Kitchen);
            await _client.AuthenticateAsAsync(cook.CPF, password);

            var response = await _client.PutAsJsonAsync(
                $"/api/kds/{order.Id}/items/{item.Id}/status",
                KdsStatus.Preparing);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);
        }
    }
}