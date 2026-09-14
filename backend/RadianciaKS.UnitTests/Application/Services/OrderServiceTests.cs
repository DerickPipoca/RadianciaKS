using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MockQueryable.Moq;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.DTOs.Payment;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.UnitTests.Application.Services
{
    public class OrderServiceTests
    {
        private readonly Mock<IApplicationDbContext> _contextMock;
        private readonly Mock<IValidator<OrderRequestDto>> _validatorMock;
        private readonly Mock<ITaxService> _taxServiceMock;
        private readonly Mock<IKdsNotificationService> _kdsNotificationMock;
        private readonly Mock<IUserProvider> _userProviderMock;
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            _contextMock = new Mock<IApplicationDbContext>();
            _validatorMock = new Mock<IValidator<OrderRequestDto>>();
            _taxServiceMock = new Mock<ITaxService>();
            _kdsNotificationMock = new Mock<IKdsNotificationService>();
            _userProviderMock = new Mock<IUserProvider>();

            _orderService = new OrderService(
                _contextMock.Object,
                _validatorMock.Object,
                _taxServiceMock.Object,
                _kdsNotificationMock.Object,
                _userProviderMock.Object
            );
        }

        private static Order CreateMockOrder(Action<Order>? configure = null)
        {
            var order = new Order
            {
                TenantId = Guid.NewGuid(),
                TableNumber = "Mesa 05",
                OrderStatus = OrderStatus.Open,
                PaymentStatus = PaymentStatus.Pending,
                TotalAmount = 85.00m,
                Items = new List<OrderItem>(),
                Payments = new List<Payment>()
            };

            configure?.Invoke(order);
            return order;
        }

        private void SetupOrdersDbSet(List<Order> orders)
        {
            var mockDbSet = orders.BuildMockDbSet();
            _contextMock.Setup(c => c.Orders).Returns(mockDbSet.Object);
        }

        private Mock<IDbContextTransaction> SetupTransactionMock()
        {
            var transactionMock = new Mock<IDbContextTransaction>();
            _contextMock
                .Setup(c => c.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(transactionMock.Object);
            return transactionMock;
        }

        private void SetupPaymentsDbSet(List<Payment>? payments = null)
        {
            var mockDbSet = (payments ?? new List<Payment>()).BuildMockDbSet();
            _contextMock.Setup(c => c.Payments).Returns(mockDbSet.Object);
        }

        private void SetupStoreSettingsDbSet(decimal serviceCharge = 10.0m)
        {
            var settings = new List<StoreSettings>
        {
            new StoreSettings { ServiceCharge = serviceCharge }
        };
            _contextMock.Setup(c => c.StoreSettings).Returns(settings.BuildMockDbSet().Object);
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

        private void SetupCashShiftsDbSet(List<CashShift> shifts)
        {
            var mockDbSet = shifts.BuildMockDbSet();
            _contextMock.Setup(c => c.CashShifts).Returns(mockDbSet.Object);
        }

        [Fact]
        public async Task CreateOrder_Should_CalculateTotals_CommitTransaction_And_NotifyKds_When_Successful()
        {
            var employeeId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var shiftId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var modifierOptionId = Guid.NewGuid();

            _userProviderMock.Setup(u => u.GetUserId()).Returns(employeeId);

            var openShift = new CashShift
            {
                Id = shiftId,
                TenantId = tenantId,
                Status = CashShiftStatus.Open
            };
            _contextMock.Setup(c => c.CashShifts)
                .Returns(new List<CashShift> { openShift }.BuildMockDbSet().Object);

            var employee = new Employee
            {
                Id = employeeId,
                TenantId = tenantId,
                Name = "Atendente Teste"
            };
            var employeesDbSet = new List<Employee> { employee }.BuildMockDbSet();
            _contextMock.Setup(c => c.Employees).Returns(employeesDbSet.Object);
            _contextMock.Setup(c => c.Employees.FindAsync(employeeId)).ReturnsAsync(employee);

            var product = new Product
            {
                Id = productId,
                Name = "Burger Duplo",
                Price = 30.00m
            };
            var productsDbSet = new List<Product> { product }.BuildMockDbSet();
            _contextMock.Setup(c => c.Products).Returns(productsDbSet.Object);
            _contextMock.Setup(c => c.Products.FindAsync(productId)).ReturnsAsync(product);

            var modifierOption = new ModifierOption
            {
                Id = modifierOptionId,
                Name = "Queijo Extra",
                AdditionalPrice = 5.00m,
                ModifierGroup = new ModifierGroup { Name = "Adicionais" }
            };
            _contextMock.Setup(c => c.ModifierOptions)
                .Returns(new List<ModifierOption> { modifierOption }.BuildMockDbSet().Object);

            _contextMock.Setup(c => c.Promotions)
                .Returns(new List<Promotion>().BuildMockDbSet().Object);

            var storeSettings = new StoreSettings { ServiceCharge = 10.0m };
            _contextMock.Setup(c => c.StoreSettings)
                .Returns(new List<StoreSettings> { storeSettings }.BuildMockDbSet().Object);

            var ordersDbSet = new List<Order>().BuildMockDbSet();
            _contextMock.Setup(c => c.Orders).Returns(ordersDbSet.Object);
            var transactionMock = SetupTransactionMock();

            // DTO de entrada: 2 Burgers (30 cada) + Queijo Extra (5 cada) = 35 * 2 = 70.00 Subtotal
            // Taxa de 10% = 7.00 => TotalAmount esperado = 77.00
            var requestDto = new OrderRequestDto
            {
                TableNumber = "Mesa 01",
                ApplyServiceFee = true,
                Items =
                [
                    new OrderItemRequestDto
                {
                    ProductId = productId,
                    Quantity = 2,
                    SelectedModifierIds = [modifierOptionId]
                }
                ],
                Payments = []
            };

            var result = await _orderService.CreateOrder(requestDto);

            result.ShouldNotBeNull();
            result.TotalAmount.ShouldBe(77.00m);
            result.TableNumber.ShouldBe("Mesa 01");

            _contextMock.Verify(c => c.Orders.AddAsync(It.Is<Order>(o =>
                o.TotalAmount == 77.00m &&
                o.CashShiftId == shiftId &&
                o.EmployeeId == employeeId), default), Times.Once);

            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
            transactionMock.Verify(t => t.CommitAsync(default), Times.Once);
            transactionMock.Verify(t => t.RollbackAsync(default), Times.Never);

            _kdsNotificationMock.Verify(k => k.NotifyOrderUpdatedAsync(
                tenantId.ToString(),
                It.Is<OrderResponseDto>(dto => dto.TotalAmount == 77.00m)), Times.Once);
        }

        [Fact]
        public async Task CreateOrder_Should_RollbackTransaction_When_SaveChangesFails()
        {
            var employeeId = Guid.NewGuid();
            var tenantId = Guid.NewGuid();
            var productId = Guid.NewGuid();

            _userProviderMock.Setup(u => u.GetUserId()).Returns(employeeId);

            var openShift = new CashShift { Id = Guid.NewGuid(), Status = CashShiftStatus.Open };
            _contextMock.Setup(c => c.CashShifts).Returns(new List<CashShift> { openShift }.BuildMockDbSet().Object);

            var employee = new Employee { Id = employeeId, TenantId = tenantId };
            _contextMock.Setup(c => c.Employees).Returns(new List<Employee> { employee }.BuildMockDbSet().Object);
            _contextMock.Setup(c => c.Employees.FindAsync(employeeId)).ReturnsAsync(employee);

            var product = new Product { Id = productId, Price = 20.00m };
            _contextMock.Setup(c => c.Products).Returns(new List<Product> { product }.BuildMockDbSet().Object);
            _contextMock.Setup(c => c.Products.FindAsync(productId)).ReturnsAsync(product);

            _contextMock.Setup(c => c.StoreSettings).Returns(new List<StoreSettings>().BuildMockDbSet().Object);
            _contextMock.Setup(c => c.Orders).Returns(new List<Order>().BuildMockDbSet().Object);

            var transactionMock = SetupTransactionMock();

            _contextMock.Setup(c => c.SaveChangesAsync(default))
                .ThrowsAsync(new DbUpdateException("Erro de persistência no banco."));

            var requestDto = new OrderRequestDto
            {
                TableNumber = "Mesa 02",
                Items = [new OrderItemRequestDto { ProductId = productId, Quantity = 1 }]
            };

            await Should.ThrowAsync<DbUpdateException>(() => _orderService.CreateOrder(requestDto));

            transactionMock.Verify(t => t.CommitAsync(default), Times.Never);
            transactionMock.Verify(t => t.RollbackAsync(default), Times.Once);
            _kdsNotificationMock.Verify(k => k.NotifyOrderUpdatedAsync(It.IsAny<string>(), It.IsAny<OrderResponseDto>()), Times.Never);
        }

        [Fact]
        public async Task CreateOrder_Should_ThrowUnauthorizedAccessException_When_UserIsNotAuthenticated()
        {
            _userProviderMock.Setup(u => u.GetUserId()).Returns((Guid?)null);
            var dto = new OrderRequestDto();

            var exception = await Should.ThrowAsync<UnauthorizedAccessException>(() =>
                _orderService.CreateOrder(dto));

            exception.Message.ShouldContain("Usuário não autenticado.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task CreateOrder_Should_ThrowException_When_NoCashShiftIsOpen()
        {
            var employeeId = Guid.NewGuid();
            _userProviderMock.Setup(u => u.GetUserId()).Returns(employeeId);

            var emptyShifts = new List<CashShift>();
            _contextMock.Setup(c => c.CashShifts).Returns(emptyShifts.BuildMockDbSet().Object);

            var dto = new OrderRequestDto();

            var exception = await Should.ThrowAsync<Exception>(() =>
                _orderService.CreateOrder(dto));

            exception.Message.ShouldContain("O caixa está fechado. Não é possível realizar vendas.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task CreateOrder_Should_ThrowException_When_EmployeeNotFoundInDatabase()
        {
            var employeeId = Guid.NewGuid();
            _userProviderMock.Setup(u => u.GetUserId()).Returns(employeeId);

            var openShift = new CashShift
            {
                Id = Guid.NewGuid(),
                Status = CashShiftStatus.Open
            };
            _contextMock.Setup(c => c.CashShifts).Returns(new List<CashShift> { openShift }.BuildMockDbSet().Object);

            var mockEmployeesDbSet = new List<Employee>().BuildMockDbSet();
            _contextMock.Setup(c => c.Employees).Returns(mockEmployeesDbSet.Object);
            _contextMock.Setup(c => c.Employees.FindAsync(employeeId)).ReturnsAsync((Employee?)null);

            var dto = new OrderRequestDto();

            var exception = await Should.ThrowAsync<Exception>(() =>
                _orderService.CreateOrder(dto));

            exception.Message.ShouldContain("Funcionário não encontrado no banco de dados.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task CheckoutOrder_Should_PayOrder_GenerateNfce_And_ReturnChangeAmount_When_Successful()
        {
            var employeeId = Guid.NewGuid();
            _userProviderMock.Setup(u => u.GetUserId()).Returns(employeeId);

            var item = CreateMockOrderItem(i =>
            {
                i.UnitPrice = 50.00m;
                i.Quantity = 2; // Subtotal = 100.00m
            });

            var order = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Open;
                o.PaymentStatus = PaymentStatus.Pending;
                o.TotalAmount = 100.00m;
                o.Items = new List<OrderItem> { item };
                o.Payments = new List<Payment>();
            });

            SetupOrdersDbSet(new List<Order> { order });
            SetupPaymentsDbSet();
            SetupStoreSettingsDbSet(serviceCharge: 10.0m);

            const string fakeNfceUrl = "https://df-e.fazenda.pr.gov.br/nfce/qrcode?p=fake123";
            _taxServiceMock
                .Setup(t => t.GenerateNfceAsync(It.Is<Order>(ord => ord.Id == order.Id)))
                .ReturnsAsync(fakeNfceUrl);

            // Subtotal = 100.00, sem taxa de serviço => TotalAmount = 100.00
            // Pago em dinheiro: 120.00 => Troco esperado = 20.00
            var checkoutDto = new CheckoutRequestDto
            {
                ApplyServiceFee = false,
                Payments =
                [
                    new PaymentRequestDto
                {
                    Amount = 120.00m,
                    Method = PaymentMethod.Cash
                }
                ]
            };

            var result = await _orderService.CheckoutOrder(order.Id, checkoutDto);

            result.ShouldNotBeNull();
            result.ChangeAmount.ShouldBe(20.00m);
            result.PaymentStatus.ShouldBe(PaymentStatus.Paid);

            order.PaymentStatus.ShouldBe(PaymentStatus.Paid);
            order.PaidById.ShouldBe(employeeId);
            order.ReceiptUrl.ShouldBe(fakeNfceUrl);

            _taxServiceMock.Verify(t => t.GenerateNfceAsync(order), Times.Once);
            _contextMock.Verify(c => c.Payments.Add(It.IsAny<Payment>()), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task CheckoutOrder_Should_ApplyServiceFee_When_Requested()
        {
            var employeeId = Guid.NewGuid();
            _userProviderMock.Setup(u => u.GetUserId()).Returns(employeeId);

            var item = CreateMockOrderItem(i =>
            {
                i.UnitPrice = 100.00m;
                i.Quantity = 1; // Subtotal = 100.00m
            });

            var order = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Open;
                o.PaymentStatus = PaymentStatus.Pending;
                o.Items = new List<OrderItem> { item };
            });

            SetupOrdersDbSet(new List<Order> { order });
            SetupPaymentsDbSet();
            SetupStoreSettingsDbSet(serviceCharge: 10.0m); // 10%

            _taxServiceMock
                .Setup(t => t.GenerateNfceAsync(It.IsAny<Order>()))
                .ReturnsAsync("https://receipt.radiancia.com/123");

            // Subtotal 100.00 + 10% (10.00) = 110.00 TotalAmount
            var checkoutDto = new CheckoutRequestDto
            {
                ApplyServiceFee = true,
                Payments =
                [
                    new PaymentRequestDto
                {
                    Amount = 110.00m,
                    Method = PaymentMethod.CreditCard
                }
                ]
            };

            var result = await _orderService.CheckoutOrder(order.Id, checkoutDto);

            order.TotalAmount.ShouldBe(110.00m);
            order.ServiceFeeAmount.ShouldBe(10.00m);
            result.TotalAmount.ShouldBe(110.00m);
            result.ChangeAmount.ShouldBe(0.00m);
        }

        [Fact]
        public async Task CheckoutOrder_Should_ThrowUnauthorizedAccessException_When_UserIsNotAuthenticated()
        {
            _userProviderMock.Setup(u => u.GetUserId()).Returns((Guid?)null);

            var order = CreateMockOrder();
            SetupOrdersDbSet(new List<Order> { order });

            var checkoutDto = new CheckoutRequestDto();

            var exception = await Should.ThrowAsync<UnauthorizedAccessException>(() =>
                _orderService.CheckoutOrder(order.Id, checkoutDto));

            exception.Message.ShouldContain("Usuário não autenticado.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task CheckoutOrder_Should_ThrowArgumentException_When_OrderIsAlreadyPaid()
        {
            var employeeId = Guid.NewGuid();
            _userProviderMock.Setup(u => u.GetUserId()).Returns(employeeId);

            var order = CreateMockOrder(o => o.PaymentStatus = PaymentStatus.Paid);
            SetupOrdersDbSet(new List<Order> { order });

            var checkoutDto = new CheckoutRequestDto();

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _orderService.CheckoutOrder(order.Id, checkoutDto));

            exception.Message.ShouldContain("Pedido já pago.");
            _taxServiceMock.Verify(t => t.GenerateNfceAsync(It.IsAny<Order>()), Times.Never);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task CheckoutOrder_Should_ThrowArgumentException_When_PaymentIsInsufficient()
        {
            var employeeId = Guid.NewGuid();
            _userProviderMock.Setup(u => u.GetUserId()).Returns(employeeId);

            var item = CreateMockOrderItem(i =>
            {
                i.UnitPrice = 50.00m;
                i.Quantity = 1;
            });

            var order = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Open;
                o.PaymentStatus = PaymentStatus.Pending;
                o.Items = new List<OrderItem> { item };
            });

            SetupOrdersDbSet(new List<Order> { order });
            SetupPaymentsDbSet();
            SetupStoreSettingsDbSet();

            // TotalAmount é 50.00, mas o pagamento é apenas 30.00
            var checkoutDto = new CheckoutRequestDto
            {
                ApplyServiceFee = false,
                Payments =
                [
                    new PaymentRequestDto
                {
                    Amount = 30.00m,
                    Method = PaymentMethod.Pix
                }
                ]
            };

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _orderService.CheckoutOrder(order.Id, checkoutDto));

            exception.Message.ShouldContain("Valor pago é insuficiente.");
            _taxServiceMock.Verify(t => t.GenerateNfceAsync(It.IsAny<Order>()), Times.Never);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task GetOrderById_Should_ReturnOrderResponseDto_When_OrderExists()
        {
            var targetOrder = CreateMockOrder();
            SetupOrdersDbSet(new List<Order> { targetOrder });

            var result = await _orderService.GetOrderById(targetOrder.Id);

            result.ShouldNotBeNull();
            result.Id.ShouldBe(targetOrder.Id);
            result.TableNumber.ShouldBe("Mesa 05");
        }

        [Fact]
        public async Task GetOrderById_Should_ThrowArgumentException_When_OrderDoesNotExist()
        {
            SetupOrdersDbSet(new List<Order>());

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _orderService.GetOrderById(Guid.NewGuid()));

            exception.Message.ShouldContain("Pedido não encontrado.");
        }

        [Fact]
        public async Task CancelOrder_Should_SetStatusToCanceled_And_NotifyKds_When_Valid()
        {
            var order = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Open;
                o.PaymentStatus = PaymentStatus.Pending;
            });

            SetupOrdersDbSet(new List<Order> { order });

            var result = await _orderService.CancelOrder(order.Id);

            result.OrderStatus.ShouldBe(OrderStatus.Canceled);
            order.OrderStatus.ShouldBe(OrderStatus.Canceled);

            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
            _kdsNotificationMock.Verify(k => k.NotifyOrderCanceledAsync(
                order.TenantId.ToString(),
                It.Is<OrderResponseDto>(dto => dto.Id == order.Id)),
                Times.Once);
        }

        [Fact]
        public async Task CancelOrder_Should_ThrowArgumentException_When_OrderIsAlreadyPaid()
        {
            var order = CreateMockOrder(o =>
            {
                o.PaymentStatus = PaymentStatus.Paid;
            });

            SetupOrdersDbSet(new List<Order> { order });

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _orderService.CancelOrder(order.Id));

            exception.Message.ShouldContain("Não é possível cancelar um pedido já pago.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task CancelOrder_Should_ThrowArgumentException_When_OrderIsRefunded()
        {
            var order = CreateMockOrder(o =>
            {
                o.PaymentStatus = PaymentStatus.Refunded;
            });

            SetupOrdersDbSet(new List<Order> { order });

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _orderService.CancelOrder(order.Id));

            exception.Message.ShouldContain("Não é possível cancelar um pedido estornado.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task DeliverOrder_Should_SetStatusToDelivered_And_NotifyKds_When_Valid()
        {
            var order = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.ReadyToServe;
                o.PaymentStatus = PaymentStatus.Paid;
            });

            SetupOrdersDbSet(new List<Order> { order });

            var result = await _orderService.DeliverOrder(order.Id);

            result.OrderStatus.ShouldBe(OrderStatus.Delivered);
            order.OrderStatus.ShouldBe(OrderStatus.Delivered);

            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
            _kdsNotificationMock.Verify(k => k.NotifyDeliveredItemAsync(
                order.TenantId.ToString(),
                It.Is<OrderResponseDto>(dto => dto.Id == order.Id)),
                Times.Once);
        }

        [Fact]
        public async Task DeliverOrder_Should_ThrowArgumentException_When_OrderIsCanceled()
        {
            var order = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Canceled;
            });

            SetupOrdersDbSet(new List<Order> { order });

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _orderService.DeliverOrder(order.Id));

            exception.Message.ShouldContain("Não é possível entregar um pedido cancelado.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        private void SetupOrderItemsDbSet(List<OrderItem>? items = null)
        {
            var mockDbSet = (items ?? new List<OrderItem>()).BuildMockDbSet();
            _contextMock.Setup(c => c.OrderItems).Returns(mockDbSet.Object);
        }

        private static OrderItem CreateMockOrderItem(Action<OrderItem>? configure = null)
        {
            var item = new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                ProductId = Guid.NewGuid(),
                UnitPrice = 25.00m,
                Quantity = 2,
                KdsStatus = KdsStatus.Pending,
                Product = new Product { Id = Guid.NewGuid(), Name = "Hambúrguer Clássico", Price = 25.00m },
                SelectedModifiers = new List<OrderItemModifier>()
            };

            configure?.Invoke(item);
            return item;
        }

        [Fact]
        public async Task RemoveItemFromOrder_Should_RemoveItemAndSubtractAmount_When_Valid()
        {
            // Arrange
            var itemToRemove = CreateMockOrderItem(i =>
            {
                i.UnitPrice = 20.00m;
                i.Quantity = 2; // Total do item = 40.00
            });

            var remainingItem = CreateMockOrderItem(i =>
            {
                i.UnitPrice = 15.00m;
                i.Quantity = 1; // Total do item = 15.00
            });

            var order = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Open;
                o.PaymentStatus = PaymentStatus.Pending;
                o.TotalAmount = 55.00m;
                o.Items = new List<OrderItem> { itemToRemove, remainingItem };
            });

            SetupOrdersDbSet(new List<Order> { order });
            SetupOrderItemsDbSet(order.Items.ToList());

            var result = await _orderService.RemoveItemFromOrder(order.Id, itemToRemove.Id);

            order.TotalAmount.ShouldBe(15.00m);
            result.TotalAmount.ShouldBe(15.00m);

            _contextMock.Verify(c => c.OrderItems.Remove(It.Is<OrderItem>(i => i.Id == itemToRemove.Id)), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task RemoveItemFromOrder_Should_ThrowArgumentException_When_OrderIsCanceled()
        {
            var item = CreateMockOrderItem();
            var order = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Canceled;
                o.Items = new List<OrderItem> { item };
            });

            SetupOrdersDbSet(new List<Order> { order });

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _orderService.RemoveItemFromOrder(order.Id, item.Id));

            exception.Message.ShouldContain("Incapaz de remover qualquer item deste pedido.");
            _contextMock.Verify(c => c.OrderItems.Remove(It.IsAny<OrderItem>()), Times.Never);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task RemoveItemFromOrder_Should_ThrowArgumentException_When_OrderIsPaid()
        {
            var item = CreateMockOrderItem();
            var order = CreateMockOrder(o =>
            {
                o.PaymentStatus = PaymentStatus.Paid;
                o.Items = new List<OrderItem> { item };
            });

            SetupOrdersDbSet(new List<Order> { order });

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _orderService.RemoveItemFromOrder(order.Id, item.Id));

            exception.Message.ShouldContain("Incapaz de remover qualquer item deste pedido.");
            _contextMock.Verify(c => c.OrderItems.Remove(It.IsAny<OrderItem>()), Times.Never);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task RemoveItemFromOrder_Should_ThrowArgumentException_When_ItemDoesNotExistInOrder()
        {
            var existingItem = CreateMockOrderItem();
            var order = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Open;
                o.PaymentStatus = PaymentStatus.Pending;
                o.Items = new List<OrderItem> { existingItem };
            });

            SetupOrdersDbSet(new List<Order> { order });

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _orderService.RemoveItemFromOrder(order.Id, Guid.NewGuid()));

            exception.Message.ShouldContain("Item não encontrado no pedido.");
            _contextMock.Verify(c => c.OrderItems.Remove(It.IsAny<OrderItem>()), Times.Never);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task UpdateItemStatus_Should_SetOrderToPreparing_When_StatusIsPreparingAndOrderIsOpen()
        {
            var item = CreateMockOrderItem(i => i.KdsStatus = KdsStatus.Pending);
            var order = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Open;
                o.Items = new List<OrderItem> { item };
            });

            SetupOrdersDbSet(new List<Order> { order });

            var result = await _orderService.UpdateItemStatus(order.Id, item.Id, KdsStatus.Preparing);

            item.KdsStatus.ShouldBe(KdsStatus.Preparing);
            order.OrderStatus.ShouldBe(OrderStatus.Preparing);
            result.OrderStatus.ShouldBe(OrderStatus.Preparing);

            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
            _kdsNotificationMock.Verify(k => k.NotifyOrderUpdatedAsync(
                order.TenantId.ToString(),
                It.Is<OrderResponseDto>(dto => dto.Id == order.Id)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateItemStatus_Should_SetOrderToReadyToServe_When_AllItemsBecomeDone()
        {
            var item1 = CreateMockOrderItem(i => i.KdsStatus = KdsStatus.Done);
            var item2 = CreateMockOrderItem(i => i.KdsStatus = KdsStatus.Preparing);

            var order = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Preparing;
                o.Items = new List<OrderItem> { item1, item2 };
            });

            SetupOrdersDbSet(new List<Order> { order });

            var result = await _orderService.UpdateItemStatus(order.Id, item2.Id, KdsStatus.Done);

            item2.KdsStatus.ShouldBe(KdsStatus.Done);
            order.OrderStatus.ShouldBe(OrderStatus.ReadyToServe);
            result.OrderStatus.ShouldBe(OrderStatus.ReadyToServe);

            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
            _kdsNotificationMock.Verify(k => k.NotifyOrderUpdatedAsync(
                order.TenantId.ToString(),
                It.Is<OrderResponseDto>(dto => dto.Id == order.Id)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateItemStatus_Should_NotChangeOrderToReadyToServe_When_OrderIsCanceled()
        {
            var item = CreateMockOrderItem(i => i.KdsStatus = KdsStatus.Preparing);
            var order = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Canceled;
                o.Items = new List<OrderItem> { item };
            });

            SetupOrdersDbSet(new List<Order> { order });

            var result = await _orderService.UpdateItemStatus(order.Id, item.Id, KdsStatus.Done);

            item.KdsStatus.ShouldBe(KdsStatus.Done);
            order.OrderStatus.ShouldBe(OrderStatus.Canceled);
            result.OrderStatus.ShouldBe(OrderStatus.Canceled);

            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task UpdateItemStatus_Should_ThrowArgumentException_When_ItemNotFound()
        {
            var item = CreateMockOrderItem();
            var order = CreateMockOrder(o =>
            {
                o.Items = new List<OrderItem> { item };
            });

            SetupOrdersDbSet(new List<Order> { order });

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _orderService.UpdateItemStatus(order.Id, Guid.NewGuid(), KdsStatus.Done));

            exception.Message.ShouldContain("Item não encontrado no pedido.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task AddItemToOrder_Should_AppendItem_RecalculateTotals_And_NotifyKds()
        {
            var productId = Guid.NewGuid();
            var existingItem = CreateMockOrderItem(i =>
            {
                i.UnitPrice = 30.00m;
                i.Quantity = 1; // Subtotal inicial = 30.00
            });

            var order = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Open;
                o.PaymentStatus = PaymentStatus.Pending;
                o.ServiceFeePercentage = 10.0m;
                o.TotalAmount = 33.00m; // 30.00 + 10%
                o.Items = new List<OrderItem> { existingItem };
            });

            SetupOrdersDbSet(new List<Order> { order });

            var newProduct = new Product
            {
                Id = productId,
                Name = "Batata Frita",
                Price = 20.00m
            };
            _contextMock.Setup(c => c.Products.FindAsync(productId)).ReturnsAsync(newProduct);
            _contextMock.Setup(c => c.Promotions).Returns(new List<Promotion>().BuildMockDbSet().Object);

            var itemDto = new OrderItemRequestDto
            {
                ProductId = productId,
                Quantity = 2 // 20.00 * 2 = 40.00
            };

            // Subtotal final: 30.00 + 40.00 = 70.00
            // Taxa 10%: 7.00 => TotalAmount final = 77.00
            var result = await _orderService.AddItemToOrder(order.Id, itemDto);

            result.ShouldNotBeNull();
            order.Items.Count.ShouldBe(2);
            order.TotalAmount.ShouldBe(77.00m);
            order.ServiceFeeAmount.ShouldBe(7.00m);
            result.TotalAmount.ShouldBe(77.00m);

            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
            _kdsNotificationMock.Verify(k => k.NotifyOrderUpdatedAsync(
                order.TenantId.ToString(),
                It.Is<OrderResponseDto>(dto => dto.TotalAmount == 77.00m)),
                Times.Once);
        }

        [Fact]
        public async Task AddItemsToOrder_Should_RevertStatusToPreparing_When_OrderWasReadyToServe()
        {
            var productId = Guid.NewGuid();
            var order = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.ReadyToServe; // Cozinha já havia terminado
                o.PaymentStatus = PaymentStatus.Pending;
                o.ServiceFeePercentage = 0m;
                o.TotalAmount = 50.00m;
                o.Items = new List<OrderItem> { CreateMockOrderItem(i => { i.UnitPrice = 50.00m; i.Quantity = 1; }) };
            });

            SetupOrdersDbSet(new List<Order> { order });
            SetupOrderItemsDbSet();

            var product = new Product { Id = productId, Name = "Sobremesa", Price = 15.00m };
            _contextMock.Setup(c => c.Products.FindAsync(productId)).ReturnsAsync(product);
            _contextMock.Setup(c => c.Promotions).Returns(new List<Promotion>().BuildMockDbSet().Object);

            var itemsDto = new List<OrderItemRequestDto>
            {
                new OrderItemRequestDto { ProductId = productId, Quantity = 1 }
            };

            var result = await _orderService.AddItemsToOrder(order.Id, itemsDto);

            order.OrderStatus.ShouldBe(OrderStatus.Preparing);
            result.OrderStatus.ShouldBe(OrderStatus.Preparing);

            _contextMock.Verify(c => c.OrderItems.Add(It.Is<OrderItem>(i => i.KdsStatus == KdsStatus.Pending)), Times.Once);
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task AddItemsToOrder_Should_ChangePaymentStatusToPartial_When_OrderWasPaidAndTotalIncreases()
        {
            var productId = Guid.NewGuid();
            var order = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Open;
                o.PaymentStatus = PaymentStatus.Paid;
                o.ServiceFeePercentage = 0m;
                o.TotalAmount = 50.00m;
                o.Items = new List<OrderItem> { CreateMockOrderItem(i => { i.UnitPrice = 50.00m; i.Quantity = 1; }) };
                o.Payments = new List<Payment> { new Payment { Amount = 50.00m, Method = PaymentMethod.CreditCard } };
            });

            SetupOrdersDbSet(new List<Order> { order });
            SetupOrderItemsDbSet();

            var product = new Product { Id = productId, Name = "Chopp 500ml", Price = 12.00m };
            _contextMock.Setup(c => c.Products.FindAsync(productId)).ReturnsAsync(product);
            _contextMock.Setup(c => c.Promotions).Returns(new List<Promotion>().BuildMockDbSet().Object);

            var itemsDto = new List<OrderItemRequestDto>
            {
                new OrderItemRequestDto { ProductId = productId, Quantity = 1 }
            };

            var result = await _orderService.AddItemsToOrder(order.Id, itemsDto);

            // Novo total: 62.00. Como o total pago é 50.00, o status deve ir para Partial
            order.PaymentStatus.ShouldBe(PaymentStatus.Partial);
            result.PaymentStatus.ShouldBe(PaymentStatus.Partial);

            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Once);
        }

        [Fact]
        public async Task AddItemToOrder_Should_ThrowArgumentException_When_ProductNotFound()
        {
            var order = CreateMockOrder();
            SetupOrdersDbSet(new List<Order> { order });

            var invalidProductId = Guid.NewGuid();
            _contextMock.Setup(c => c.Products.FindAsync(invalidProductId)).ReturnsAsync((Product?)null);

            var itemDto = new OrderItemRequestDto { ProductId = invalidProductId, Quantity = 1 };

            var exception = await Should.ThrowAsync<ArgumentException>(() =>
                _orderService.AddItemToOrder(order.Id, itemDto));

            exception.Message.ShouldContain("Produto não encontrado.");
            _contextMock.Verify(c => c.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task GetPendingKdsOrdersAsync_Should_ReturnOnlyOpenAndPreparingOrders_OrderedChronologically()
        {
            // Arrange
            var baseDate = DateTime.UtcNow;

            var openOlder = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Open;
                SetCreatedAt(o, baseDate.AddMinutes(-30));
            });

            var preparingNewer = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Preparing;
                SetCreatedAt(o, baseDate.AddMinutes(-10));
            });

            var deliveredOrder = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Delivered;
                SetCreatedAt(o, baseDate.AddMinutes(-40));
            });

            var canceledOrder = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Canceled;
                SetCreatedAt(o, baseDate.AddMinutes(-50));
            });

            var readyToServeOrder = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.ReadyToServe;
                SetCreatedAt(o, baseDate.AddMinutes(-20));
            });

            SetupOrdersDbSet(new List<Order>
        {
            openOlder,
            preparingNewer,
            deliveredOrder,
            canceledOrder,
            readyToServeOrder
        });

            // Act
            var result = (await _orderService.GetPendingKdsOrdersAsync()).ToList();

            // Assert
            result.Count.ShouldBe(2);
            result[0].Id.ShouldBe(openOlder.Id);
            result[1].Id.ShouldBe(preparingNewer.Id);
        }

        [Fact]
        public async Task GetAllOrders_Should_FilterBySearchTerm_ForTableNumber()
        {
            var order1 = CreateMockOrder(o => o.TableNumber = "Mesa VIP");
            var order2 = CreateMockOrder(o => o.TableNumber = "Balcão 01");
            var order3 = CreateMockOrder(o => o.TableNumber = "Mesa 02");

            SetupOrdersDbSet(new List<Order> { order1, order2, order3 });

            var parameters = new OrderQueryParameters
            {
                SearchTerm = "vip",
                PageNumber = 1,
                PageSize = 10
            };

            var result = await _orderService.GetAllOrders(parameters);

            result.TotalRecords.ShouldBe(1);
            result.Data.Count().ShouldBe(1);
            result.Data.First().TableNumber.ShouldBe("Mesa VIP");
        }

        [Fact]
        public async Task GetAllOrders_Should_FilterByStatus_And_PaginateResults()
        {
            var orders = new List<Order>();
            for (int i = 1; i <= 5; i++)
            {
                orders.Add(CreateMockOrder(o =>
                {
                    o.OrderStatus = OrderStatus.Delivered;
                    o.PaymentStatus = PaymentStatus.Paid;
                    o.TableNumber = $"Mesa {i:D2}";
                    SetCreatedAt(o, DateTime.UtcNow.AddMinutes(i));
                }));
            }

            orders.Add(CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Open;
                o.PaymentStatus = PaymentStatus.Pending;
            }));

            SetupOrdersDbSet(orders);

            var parameters = new OrderQueryParameters
            {
                OrderStatus = OrderStatus.Delivered,
                PaymentStatus = PaymentStatus.Paid,
                PageNumber = 2,
                PageSize = 2
            };

            var result = await _orderService.GetAllOrders(parameters);

            result.TotalRecords.ShouldBe(5);
            result.PageNumber.ShouldBe(2);
            result.PageSize.ShouldBe(2);
            result.Data.Count().ShouldBe(2);
        }

        [Fact]
        public async Task GetDashboardMetricsAsync_Should_ReturnEmptyDto_When_NoShiftFound()
        {
            SetupCashShiftsDbSet(new List<CashShift>());

            var result = await _orderService.GetDashboardMetricsAsync(Guid.NewGuid());

            result.ShouldNotBeNull();
            result.TotalOrders.ShouldBe(0);
            result.TotalRevenue.ShouldBe(0m);
            result.AverageTicket.ShouldBe(0m);
            result.TopSellingItems.ShouldBeEmpty();
        }

        [Fact]
        public async Task GetDashboardMetricsAsync_Should_CalculateAggregatedMetrics_ForActiveShift()
        {
            var waiter = new Employee { Id = Guid.NewGuid(), Name = "Carlos Garçom", Role = EmployeeRole.Waiter };
            var opener = new Employee { Id = Guid.NewGuid(), Name = "Gerente Ana", Role = EmployeeRole.Manager };

            var burgerProduct = new Product { Id = Guid.NewGuid(), Name = "Hambúrguer Artesanal", Price = 35.00m };
            var friesProduct = new Product { Id = Guid.NewGuid(), Name = "Batata Frita", Price = 15.00m };

            var baseTime = new DateTime(2026, 9, 14, 12, 10, 0, DateTimeKind.Utc);

            // Pedido Pago 1: 2x Burger (70.00) + 10.00 taxa = 80.00 (Pago via Pix)
            var paidOrder1 = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Delivered;
                o.PaymentStatus = PaymentStatus.Paid;
                o.TotalAmount = 80.00m;
                o.ServiceFeeAmount = 10.00m;
                o.Employee = waiter;
                SetCreatedAt(o, baseTime);
                o.Items =
                [
                    new OrderItem
                {
                    Product = burgerProduct,
                    Quantity = 2,
                    UnitPrice = 35.00m,
                    SelectedModifiers = []
                }
                ];
                o.Payments = [new Payment { Amount = 80.00m, Method = PaymentMethod.Pix }];
            });

            // Pedido Pago 2: 1x Fritas (15.00) = 15.00 (Pago via Cash)
            var paidOrder2 = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Delivered;
                o.PaymentStatus = PaymentStatus.Paid;
                o.TotalAmount = 15.00m;
                o.ServiceFeeAmount = 0m;
                o.Employee = waiter;
                SetCreatedAt(o, baseTime.AddMinutes(15));
                o.Items =
                [
                    new OrderItem
                {
                    Product = friesProduct,
                    Quantity = 1,
                    UnitPrice = 15.00m,
                    SelectedModifiers = []
                }
                ];
                o.Payments = [new Payment { Amount = 15.00m, Method = PaymentMethod.Cash }];
            });

            // Pedido Cancelado: 45.00
            var canceledOrder = CreateMockOrder(o =>
            {
                o.OrderStatus = OrderStatus.Canceled;
                o.PaymentStatus = PaymentStatus.Pending;
                o.TotalAmount = 45.00m;
                SetCreatedAt(o, baseTime.AddMinutes(5));
            });

            var shiftId = Guid.NewGuid();
            var currentShift = new CashShift
            {
                Id = shiftId,
                Active = true,
                Status = CashShiftStatus.Open,
                EmployeeOpener = opener,
                InitialBalance = 200.00m,
                Orders = [paidOrder1, paidOrder2, canceledOrder]
            };
            SetCreatedAt(currentShift, baseTime.AddHours(-1));

            SetupCashShiftsDbSet(new List<CashShift> { currentShift });

            // Act (busca o turno aberto atual)
            var result = await _orderService.GetDashboardMetricsAsync(null);

            // Assert
            result.ShouldNotBeNull();
            result.TotalOrders.ShouldBe(2);
            result.TotalRevenue.ShouldBe(95.00m); // 80 + 15
            result.AverageTicket.ShouldBe(47.50m); // 95 / 2
            result.CanceledOrdersCount.ShouldBe(1);
            result.CanceledOrdersAmount.ShouldBe(45.00m);
            result.ServiceFeeBalance.ShouldBe(10.00m);
            result.OpenedByName.ShouldBe("Gerente Ana");

            // TopSellingItems
            result.TopSellingItems.Count.ShouldBe(2);
            result.TopSellingItems.First().ProductName.ShouldBe("Hambúrguer Artesanal");
            result.TopSellingItems.First().QuantitySold.ShouldBe(2);

            // CashFlow
            result.CashFlow.Count.ShouldBe(2);
            result.CashFlow.ShouldContain(cf => cf.PaymentMethod == PaymentMethod.Pix.ToString() && cf.TotalAmount == 80.00m);
            result.CashFlow.ShouldContain(cf => cf.PaymentMethod == PaymentMethod.Cash.ToString() && cf.TotalAmount == 15.00m);

            // WaiterProductivity
            result.WaiterProductivity.Count.ShouldBe(1);
            result.WaiterProductivity.First().EmployeeName.ShouldBe("Carlos Garçom");
            result.WaiterProductivity.First().CompletedTasks.ShouldBe(2);

            // Sem turno anterior
            result.PreviousShiftComparison.HasPreviousShift.ShouldBeFalse();
        }

        [Fact]
        public async Task GetDashboardMetricsAsync_Should_CalculateComparison_When_PreviousClosedShiftExists()
        {
            var baseDate = DateTime.UtcNow;

            // Turno anterior: Fechado, 1 pedido pago de 100.00
            var prevOrder = CreateMockOrder(o =>
            {
                o.PaymentStatus = PaymentStatus.Paid;
                o.TotalAmount = 100.00m;
                SetCreatedAt(o, baseDate.AddDays(-1));
            });

            var prevShift = new CashShift
            {
                Id = Guid.NewGuid(),
                Active = false,
                Status = CashShiftStatus.Closed,
                Orders = [prevOrder]
            };
            SetCreatedAt(prevShift, baseDate.AddDays(-1));

            // Turno atual: Aberto, 2 pedidos pagos somando 150.00 (75.00 cada)
            var currOrder1 = CreateMockOrder(o =>
            {
                o.PaymentStatus = PaymentStatus.Paid;
                o.TotalAmount = 75.00m;
                SetCreatedAt(o, baseDate);
            });

            var currOrder2 = CreateMockOrder(o =>
            {
                o.PaymentStatus = PaymentStatus.Paid;
                o.TotalAmount = 75.00m;
                SetCreatedAt(o, baseDate);
            });

            var currentShift = new CashShift
            {
                Id = Guid.NewGuid(),
                Active = true,
                Status = CashShiftStatus.Open,
                Orders = [currOrder1, currOrder2]
            };
            SetCreatedAt(currentShift, baseDate);

            SetupCashShiftsDbSet(new List<CashShift> { prevShift, currentShift });

            // Variação esperada:
            // Receita: ((150 - 100) / 100) * 100 = 50%
            // Pedidos: ((2 - 1) / 1) * 100 = 100%
            var result = await _orderService.GetDashboardMetricsAsync(currentShift.Id);

            result.PreviousShiftComparison.ShouldNotBeNull();
            result.PreviousShiftComparison.HasPreviousShift.ShouldBeTrue();
            result.PreviousShiftComparison.RevenuePercentage.ShouldBe(50.00m);
            result.PreviousShiftComparison.OrdersPercentage.ShouldBe(100.00m);
        }
    }
}