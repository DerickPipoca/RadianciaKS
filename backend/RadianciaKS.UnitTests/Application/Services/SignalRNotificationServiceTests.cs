using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using RadianciaKS.Api.Hubs;
using RadianciaKS.Api.Services;
using RadianciaKS.Application.DTOs.Order;
using Xunit;

namespace RadianciaKS.UnitTests.Application.Services
{
    public class SignalRNotificationServiceTests
    {
        private readonly Mock<IHubContext<KdsHub>> _hubContextMock;
        private readonly Mock<IHubClients> _hubClientsMock;
        private readonly Mock<IClientProxy> _clientProxyMock;
        private readonly SignalRNotificationService _notificationService;

        public SignalRNotificationServiceTests()
        {
            _hubContextMock = new Mock<IHubContext<KdsHub>>();
            _hubClientsMock = new Mock<IHubClients>();
            _clientProxyMock = new Mock<IClientProxy>();

            _hubContextMock.Setup(h => h.Clients).Returns(_hubClientsMock.Object);
            _hubClientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxyMock.Object);

            _notificationService = new SignalRNotificationService(_hubContextMock.Object);
        }

        [Fact]
        public async Task NotifyOrderUpdatedAsync_Should_SendOnOrderUpdated_ToTenantGroup()
        {
            var tenantId = Guid.NewGuid().ToString();
            var order = new OrderResponseDto
            {
                Id = Guid.NewGuid(),
                TableNumber = "Mesa 10",
                TotalAmount = 75.00m,
                OrderStatus = OrderStatus.Preparing
            };

            await _notificationService.NotifyOrderUpdatedAsync(tenantId, order);

            _hubClientsMock.Verify(c => c.Group(tenantId), Times.Once);
            _clientProxyMock.Verify(c => c.SendCoreAsync(
                "OnOrderUpdated",
                It.Is<object?[]>(args => args.Length == 1 && ReferenceEquals(args[0], order)),
                default), Times.Once);
        }

        [Fact]
        public async Task NotifyDeliveredItemAsync_Should_SendOnItemDelivered_ToTenantGroup()
        {
            var tenantId = Guid.NewGuid().ToString();
            var deliveredPayload = new
            {
                OrderId = Guid.NewGuid(),
                ItemId = Guid.NewGuid(),
                ItemName = "Burger Duplo"
            };

            await _notificationService.NotifyDeliveredItemAsync(tenantId, deliveredPayload);

            _hubClientsMock.Verify(c => c.Group(tenantId), Times.Once);
            _clientProxyMock.Verify(c => c.SendCoreAsync(
                "OnItemDelivered",
                It.Is<object?[]>(args => args.Length == 1 && ReferenceEquals(args[0], deliveredPayload)),
                default), Times.Once);
        }

        [Fact]
        public async Task UpdateCashShiftStatusAsync_Should_SendUpdateSystemStatus_ToTenantGroup()
        {
            var tenantId = Guid.NewGuid().ToString();
            var status = CashShiftStatus.Closed;

            await _notificationService.UpdateCashShiftStatusAsync(tenantId, status);

            _hubClientsMock.Verify(c => c.Group(tenantId), Times.Once);
            _clientProxyMock.Verify(c => c.SendCoreAsync(
                "UpdateSystemStatus",
                It.Is<object?[]>(args => args.Length == 1 && (CashShiftStatus)args[0]! == CashShiftStatus.Closed),
                default), Times.Once);
        }

        [Fact]
        public async Task NotifyOrderCanceledAsync_Should_SendReceiveOrderCanceled_ToTenantGroup()
        {
            var tenantId = Guid.NewGuid().ToString();
            var order = new OrderResponseDto
            {
                Id = Guid.NewGuid(),
                TableNumber = "Balcão 02",
                OrderStatus = OrderStatus.Canceled
            };

            await _notificationService.NotifyOrderCanceledAsync(tenantId, order);

            _hubClientsMock.Verify(c => c.Group(tenantId), Times.Once);
            _clientProxyMock.Verify(c => c.SendCoreAsync(
                "ReceiveOrderCanceled",
                It.Is<object?[]>(args => args.Length == 1 && ReferenceEquals(args[0], order)),
                default), Times.Once);
        }
    }
}