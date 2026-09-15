using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.DTOs.Payment;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;
using RadianciaKS.Infrastructure.Context;
using RadianciaKS.IntegrationTests.Helpers;

namespace RadianciaKS.IntegrationTests.Controllers
{
    public class OrderControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public OrderControllerIntegrationTests(CustomWebApplicationFactory factory)
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

        private async Task<Employee> SeedEmployeeAsync(string cpf, string rawPassword, EmployeeRole role = EmployeeRole.Waiter)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

            var employee = new Employee
            {
                TenantId = TestTenantProvider.DefaultTenantId,
                Name = "Atendente de Pedidos",
                CPF = cpf,
                PasswordHash = passwordService.HashPassword(rawPassword),
                Role = role
            };

            context.Employees.Add(employee);
            await context.SaveChangesAsync();
            return employee;
        }

        private async Task<CashShift> SeedOpenCashShiftAsync(Guid employeeId)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var shift = new CashShift
            {
                TenantId = TestTenantProvider.DefaultTenantId,
                EmployeeOpenerId = employeeId,
                InitialBalance = 100.00m,
                Status = CashShiftStatus.Open,
            };

            context.CashShifts.Add(shift);
            await context.SaveChangesAsync();
            return shift;
        }

        private async Task<Product> SeedProductAsync(string name = "Pizza Margherita", decimal price = 45.00m)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var category = new Category
            {
                TenantId = TestTenantProvider.DefaultTenantId,
                Name = "Pizzaria",
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

        private async Task<Order> SeedExistingOrderAsync(Employee employee, Product product)
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var order = new Order
            {
                TenantId = TestTenantProvider.DefaultTenantId,
                TableNumber = "12",
                OrderStatus = OrderStatus.Open,
                PaymentStatus = PaymentStatus.Pending,
                EmployeeId = employee.Id,
            };

            var item = new OrderItem
            {
                TenantId = TestTenantProvider.DefaultTenantId,
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = 1,
                UnitPrice = product.Price,
                KdsStatus = KdsStatus.Pending,
            };

            order.Items.Add(item);
            order.TotalAmount = item.UnitPrice * item.Quantity;

            context.Orders.Add(order);
            context.OrderItems.Add(item);
            await context.SaveChangesAsync();

            return order;
        }

        [Fact]
        public async Task GetAllOrders_Should_Return401Unauthorized_When_NotAuthenticated()
        {
            var response = await _client.GetAsync("/api/order");

            response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task CreateOrder_Should_Return201Created_When_CashShiftIsOpenAndDataIsValid()
        {
            var password = "Senha@123";
            var waiter = await SeedEmployeeAsync("85493217030", password, EmployeeRole.Waiter);
            await SeedOpenCashShiftAsync(waiter.Id);
            var product = await SeedProductAsync("Burger Clássico", 30.00m);

            await _client.AuthenticateAsAsync(waiter.CPF, password);

            var request = new OrderRequestDto
            {
                TableNumber = "05",
                ApplyServiceFee = false,
                Items = new List<OrderItemRequestDto>
            {
                new OrderItemRequestDto
                {
                    ProductId = product.Id,
                    Quantity = 2
                }
            },
                Payments = new List<PaymentRequestDto>()
            };

            var response = await _client.PostAsJsonAsync("/api/order", request);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.Created, customMessage: responseBody);

            var created = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
            created.ShouldNotBeNull();
            created.TableNumber.ShouldBe("05");
            created.TotalAmount.ShouldBe(60.00m);
        }

        [Fact]
        public async Task GetOrderById_Should_Return200Ok_When_OrderExists()
        {
            var password = "Senha@123";
            var waiter = await SeedEmployeeAsync("52998224725", password, EmployeeRole.Waiter);
            var product = await SeedProductAsync();
            var order = await SeedExistingOrderAsync(waiter, product);

            await _client.AuthenticateAsAsync(waiter.CPF, password);

            var response = await _client.GetAsync($"/api/order/{order.Id}");

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var found = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
            found.ShouldNotBeNull();
            found.Id.ShouldBe(order.Id);
            found.TableNumber.ShouldBe("12");
        }

        [Fact]
        public async Task AddItemToOrder_Should_Return201Created_When_ItemIsValid()
        {
            var password = "Senha@123";
            var waiter = await SeedEmployeeAsync("49082887398", password, EmployeeRole.Waiter);
            var initialProduct = await SeedProductAsync("Chopp 500ml", 15.00m);
            var extraProduct = await SeedProductAsync("Batata Frita", 25.00m);
            var order = await SeedExistingOrderAsync(waiter, initialProduct);

            await _client.AuthenticateAsAsync(waiter.CPF, password);

            var newItemDto = new OrderItemRequestDto
            {
                ProductId = extraProduct.Id,
                Quantity = 1
            };

            var response = await _client.PostAsJsonAsync($"/api/order/{order.Id}/item", newItemDto);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.Created, customMessage: responseBody);

            var updatedOrder = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
            updatedOrder.ShouldNotBeNull();
            updatedOrder.TotalAmount.ShouldBe(40.00m);
        }

        [Fact]
        public async Task CancelOrder_Should_Return200Ok_When_OrderIsOpenAndNotPaid()
        {
            var password = "Senha@123";
            var manager = await SeedEmployeeAsync("63116114226", password, EmployeeRole.Manager);
            var product = await SeedProductAsync();
            var order = await SeedExistingOrderAsync(manager, product);

            await _client.AuthenticateAsAsync(manager.CPF, password);

            var response = await _client.PutAsync($"/api/order/{order.Id}/cancel", null);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var canceled = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
            canceled.ShouldNotBeNull();
            canceled.OrderStatus.ShouldBe(OrderStatus.Canceled);
        }

        [Fact]
        public async Task DeliverOrder_Should_Return200Ok_When_OrderIsValid()
        {
            var password = "Senha@123";
            var waiter = await SeedEmployeeAsync("33344455566", password, EmployeeRole.Waiter);
            var product = await SeedProductAsync();
            var order = await SeedExistingOrderAsync(waiter, product);

            await _client.AuthenticateAsAsync(waiter.CPF, password);

            var response = await _client.PutAsync($"/api/order/{order.Id}/deliver", null);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);

            var delivered = await response.Content.ReadFromJsonAsync<OrderResponseDto>();
            delivered.ShouldNotBeNull();
            delivered.OrderStatus.ShouldBe(OrderStatus.Delivered);
        }

        [Fact]
        public async Task GetDashboardMetrics_Should_Return200Ok_When_Authenticated()
        {
            var password = "Senha@123";
            var admin = await SeedEmployeeAsync("44455566677", password, EmployeeRole.Admin);
            await SeedOpenCashShiftAsync(admin.Id);

            await _client.AuthenticateAsAsync(admin.CPF, password);

            var response = await _client.GetAsync("/api/order/metrics");

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);
        }
    }
}