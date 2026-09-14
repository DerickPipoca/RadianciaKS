using MockQueryable.Moq;
using RadianciaKS.Application.DTOs;
using RadianciaKS.Application.DTOs.CashShift;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.UnitTests.Application.Services
{
    public class CashShiftServiceTests
    {
        private readonly Mock<IApplicationDbContext> _contextMock;
        private readonly Mock<IUserProvider> _userProviderMock;
        private readonly Mock<ITenantProvider> _tenantProviderMock;
        private readonly Mock<IKdsNotificationService> _notificationServiceMock;
        private readonly CashShiftService _cashShiftService;

        public CashShiftServiceTests()
        {
            _contextMock = new Mock<IApplicationDbContext>();
            _userProviderMock = new Mock<IUserProvider>();
            _tenantProviderMock = new Mock<ITenantProvider>();
            _notificationServiceMock = new Mock<IKdsNotificationService>();

            _cashShiftService = new CashShiftService(
                _contextMock.Object,
                _userProviderMock.Object,
                _tenantProviderMock.Object,
                _notificationServiceMock.Object
            );
        }

        private void SetupCashShiftsDbSet(List<CashShift> shifts)
        {
            var mockDbSet = shifts.BuildMockDbSet();
            _contextMock.Setup(c => c.CashShifts).Returns(mockDbSet.Object);
        }

        private static void SetCreatedAt(object entity, DateTime dateTime)
        {
            var type = entity.GetType();
            while (type != null)
            {
                var prop = type.GetProperty("CreatedAt",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.DeclaredOnly);

                if (prop != null)
                {
                    var setter = prop.GetSetMethod(true);
                    if (setter != null)
                    {
                        setter.Invoke(entity, [dateTime]);
                        return;
                    }
                }
                type = type.BaseType;
            }
        }


        [Fact]
        public async Task OpenShift_Should_CreateShift_And_NotifyKds_When_Valid()
        {
            var employeeId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            _userProviderMock.Setup(u => u.GetUserId()).Returns(employeeId);
            _tenantProviderMock.Setup(t => t.GetTenantId()).Returns(tenantId);

            SetupCashShiftsDbSet([]);

            var dto = new OpenCashShiftDto
            {
                InitialBalance = 150.00m
            };

            var result = await _cashShiftService.OpenShift(dto);

            result.ShouldNotBeNull();
            result.Status.ShouldBe(CashShiftStatus.Open);
            result.InitialBalance.ShouldBe(150.00m);

            _contextMock.Verify(c => c.CashShifts.Add(It.Is<CashShift>(s =>
                s.Status == CashShiftStatus.Open &&
                s.InitialBalance == 150.00m &&
                s.EmployeeOpenerId == employeeId)), Times.Once);

            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);

            _notificationServiceMock.Verify(n => n.UpdateCashShiftStatusAsync(
                tenantId.ToString(),
                CashShiftStatus.Open), Times.Once);
        }

        [Fact]
        public async Task OpenShift_Should_ThrowException_When_ShiftAlreadyOpen()
        {
            var existingOpenShift = new CashShift
            {
                Id = Guid.NewGuid(),
                Status = CashShiftStatus.Open,
                Active = true
            };

            SetupCashShiftsDbSet([existingOpenShift]);

            var dto = new OpenCashShiftDto { InitialBalance = 100.00m };

            var exception = await Should.ThrowAsync<Exception>(() =>
                _cashShiftService.OpenShift(dto));

            exception.Message.ShouldContain("Já existe um caixa aberto para este estabelecimento.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task OpenShift_Should_ThrowException_When_UserNotAuthenticated()
        {
            SetupCashShiftsDbSet([]);
            _userProviderMock.Setup(u => u.GetUserId()).Returns((Guid?)null);

            var dto = new OpenCashShiftDto { InitialBalance = 50.00m };

            var exception = await Should.ThrowAsync<Exception>(() =>
                _cashShiftService.OpenShift(dto));

            exception.Message.ShouldContain("ID do empregado não encontrado.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task CloseShift_Should_CalculateBalances_CloseShift_And_Notify_When_NoPendingOrders()
        {
            var employeeCloserId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();

            _userProviderMock.Setup(u => u.GetUserId()).Returns(employeeCloserId);
            _tenantProviderMock.Setup(t => t.GetTenantId()).Returns(tenantId);

            // Pedido pago de 80.00 (via 2 pagamentos de 40.00)
            var paidOrder = new Order
            {
                Id = Guid.NewGuid(),
                OrderStatus = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Paid,
                TotalAmount = 80.00m,
                Payments =
                [
                    new Payment { Amount = 40.00m, Method = PaymentMethod.Cash },
                new Payment { Amount = 40.00m, Method = PaymentMethod.Pix }
                ]
            };

            // Pedido cancelado (não deve impactar total nem bloquear o fechamento)
            var canceledOrder = new Order
            {
                Id = Guid.NewGuid(),
                OrderStatus = OrderStatus.Canceled,
                PaymentStatus = PaymentStatus.Pending,
                TotalAmount = 50.00m,
                Payments = []
            };

            var openShift = new CashShift
            {
                Id = Guid.NewGuid(),
                Status = CashShiftStatus.Open,
                Active = true,
                InitialBalance = 100.00m,
                Orders = [paidOrder, canceledOrder]
            };

            SetupCashShiftsDbSet([openShift]);

            // Saldo esperado: 100 (inicial) + 80 (vendas) = 180.00
            var dto = new CloseCashShiftDto
            {
                FinalReportedBalance = 180.00m
            };

            var result = await _cashShiftService.CloseShift(dto);

            result.ShouldNotBeNull();
            openShift.Status.ShouldBe(CashShiftStatus.Closed);
            openShift.FinalCalculatedBalance.ShouldBe(180.00m);
            openShift.FinalReportedBalance.ShouldBe(180.00m);
            openShift.EmployeeCloserId.ShouldBe(employeeCloserId);
            openShift.ClosedAt.ShouldNotBeNull();

            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
            _notificationServiceMock.Verify(n => n.UpdateCashShiftStatusAsync(
                tenantId.ToString(),
                CashShiftStatus.Closed), Times.Once);
        }

        [Fact]
        public async Task CloseShift_Should_ThrowException_When_NoOpenShiftExists()
        {
            SetupCashShiftsDbSet([]);

            var dto = new CloseCashShiftDto { FinalReportedBalance = 100.00m };

            var exception = await Should.ThrowAsync<Exception>(() =>
                _cashShiftService.CloseShift(dto));

            exception.Message.ShouldContain("Não há caixa aberto para ser fechado.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task CloseShift_Should_ThrowInvalidOperationException_When_PendingOrdersExist()
        {
            var pendingOrder = new Order
            {
                Id = Guid.NewGuid(),
                TableNumber = "08",
                OrderStatus = OrderStatus.Preparing,
                PaymentStatus = PaymentStatus.Pending,
                Payments = []
            };

            var openShift = new CashShift
            {
                Id = Guid.NewGuid(),
                Status = CashShiftStatus.Open,
                Active = true,
                Orders = [pendingOrder]
            };

            SetupCashShiftsDbSet([openShift]);

            var dto = new CloseCashShiftDto { FinalReportedBalance = 100.00m };

            var exception = await Should.ThrowAsync<InvalidOperationException>(() =>
                _cashShiftService.CloseShift(dto));

            exception.Message.ShouldContain("Não é possível fechar o caixa. Existem 1 comanda(s) aberta(s)");
            exception.Message.ShouldContain("Mesa 08");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task GetCurrentOpenShift_Should_ReturnNull_When_NoActiveOpenShift()
        {
            SetupCashShiftsDbSet([]);

            var result = await _cashShiftService.GetCurrentOpenShift();

            result.ShouldBeNull();
        }

        [Fact]
        public async Task GetCurrentOpenShift_Should_ReturnDtoWithCorrectPendingOrdersCount()
        {
            var pendingOrder1 = new Order { OrderStatus = OrderStatus.Open, PaymentStatus = PaymentStatus.Pending };
            var pendingOrder2 = new Order { OrderStatus = OrderStatus.Preparing, PaymentStatus = PaymentStatus.Partial };
            var paidOrder = new Order { OrderStatus = OrderStatus.Delivered, PaymentStatus = PaymentStatus.Paid };
            var canceledOrder = new Order { OrderStatus = OrderStatus.Canceled, PaymentStatus = PaymentStatus.Pending };

            var shift = new CashShift
            {
                Id = Guid.NewGuid(),
                Status = CashShiftStatus.Open,
                Active = true,
                Orders = [pendingOrder1, pendingOrder2, paidOrder, canceledOrder]
            };

            SetupCashShiftsDbSet([shift]);

            var result = await _cashShiftService.GetCurrentOpenShift();

            result.ShouldNotBeNull();
            result.PendingOrdersCount.ShouldBe(2);
        }

        [Fact]
        public async Task GetCashShiftHistoryAsync_Should_ReturnPagedHistoryWithRevenue()
        {
            var baseDate = DateTime.UtcNow;

            var shift1 = new CashShift
            {
                Id = Guid.NewGuid(),
                Status = CashShiftStatus.Closed,
                Orders =
                [
                    new Order { PaymentStatus = PaymentStatus.Paid, TotalAmount = 150.00m },
                    new Order { PaymentStatus = PaymentStatus.Pending, OrderStatus = OrderStatus.Canceled, TotalAmount = 50.00m }
                ]
            };
            SetCreatedAt(shift1, baseDate.AddDays(-2));

            var shift2 = new CashShift
            {
                Id = Guid.NewGuid(),
                Status = CashShiftStatus.Closed,
                Orders =
                [
                    new Order { PaymentStatus = PaymentStatus.Paid, TotalAmount = 220.00m }
                ]
            };
            SetCreatedAt(shift2, baseDate.AddDays(-1));

            var shift3 = new CashShift
            {
                Id = Guid.NewGuid(),
                Status = CashShiftStatus.Open,
                Orders = []
            };
            SetCreatedAt(shift3, baseDate);

            SetupCashShiftsDbSet([shift1, shift2, shift3]);

            var queryParameters = new BaseQueryParameters
            {
                PageNumber = 1,
                PageSize = 2
            };

            var result = await _cashShiftService.GetCashShiftHistoryAsync(queryParameters);

            result.ShouldNotBeNull();
            result.TotalRecords.ShouldBe(3);
            result.Data.Count().ShouldBe(2);

            // O primeiro deve ser o mais recente (shift3, que foi criado hoje)
            result.Data.First().CashShiftId.ShouldBe(shift3.Id);
            result.Data.First().TotalRevenue.ShouldBe(0m);

            // O segundo deve ser o shift2 (220.00 de receita)
            result.Data.Skip(1).First().CashShiftId.ShouldBe(shift2.Id);
            result.Data.Skip(1).First().TotalRevenue.ShouldBe(220.00m);
        }
    }
}