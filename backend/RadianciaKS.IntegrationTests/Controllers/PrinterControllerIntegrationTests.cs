using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;

namespace RadianciaKS.IntegrationTests.Controllers
{
    public class PrinterControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PrinterControllerIntegrationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            await _factory.ResetDatabaseAsync();
        }

        public Task DisposeAsync()
        {
            if (File.Exists("cupom_teste.bin"))
            {
                try
                {
                    File.Delete("cupom_teste.bin");
                }
                catch
                {
                    // Ignora bloqueios temporários de I/O na exclusão do arquivo de teste
                }
            }

            return Task.CompletedTask;
        }

        private static OrderResponseDto CreateSampleOrderDto()
        {
            return new OrderResponseDto
            {
                Id = Guid.NewGuid(),
                TableNumber = "07",
                OrderStatus = OrderStatus.Open,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 85.50m,
                CreatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public async Task PrintReceipt_Should_Return200Ok_When_PrintSucceeds()
        {
            var mockPrintService = new Mock<IPrintService>();
            mockPrintService.Setup(p => p.PrintReceiptAsync(
                It.IsAny<OrderResponseDto>(),
                It.IsAny<string>(),
                It.IsAny<byte[]?>()))
            .ReturnsAsync(true);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockPrintService.Object);
                });
            }).CreateClient();

            var orderDto = CreateSampleOrderDto();

            var response = await client.PostAsJsonAsync("/api/printer/receipt", orderDto);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.OK, customMessage: responseBody);
            responseBody.ShouldContain("Cupom gerado com sucesso");
        }

        [Fact]
        public async Task PrintReceipt_Should_Return400BadRequest_When_PrintFails()
        {
            var mockPrintService = new Mock<IPrintService>();
            mockPrintService.Setup(p => p.PrintReceiptAsync(
                It.IsAny<OrderResponseDto>(),
                It.IsAny<string>(),
                It.IsAny<byte[]?>()))
            .ReturnsAsync(false);

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockPrintService.Object);
                });
            }).CreateClient();

            var orderDto = CreateSampleOrderDto();

            var response = await client.PostAsJsonAsync("/api/printer/receipt", orderDto);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, customMessage: responseBody);
            responseBody.ShouldContain("Não foi possível completar a rotina.");
        }

        [Fact]
        public async Task PrintReceipt_Should_Return500InternalServerError_When_ServiceThrowsException()
        {
            var mockPrintService = new Mock<IPrintService>();
            mockPrintService.Setup(p => p.PrintReceiptAsync(
                It.IsAny<OrderResponseDto>(),
                It.IsAny<string>(),
                It.IsAny<byte[]?>()))
            .ThrowsAsync(new InvalidOperationException("Falha na porta de comunicação da impressora."));

            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddScoped(_ => mockPrintService.Object);
                });
            }).CreateClient();

            var orderDto = CreateSampleOrderDto();

            var response = await client.PostAsJsonAsync("/api/printer/receipt", orderDto);

            var responseBody = await response.Content.ReadAsStringAsync();
            response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError, customMessage: responseBody);
            responseBody.ShouldContain("Falha na porta de comunicação da impressora.");
        }
    }
}