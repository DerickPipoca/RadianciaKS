using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.DTOs;
using RadianciaKS.Application.DTOs.DashboardMetrics;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.Extensions;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Mappers;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly IApplicationDbContext _context;
        private readonly IValidator<OrderRequestDto> _validator;
        private readonly OrderMapper _mapper;
        private readonly OrderItemMapper _orderItemMapper;
        private readonly PaymentMapper _paymentMapper;
        private readonly ITaxService _taxService;
        private readonly IKdsNotificationService _kdsNotification;
        private readonly IUserProvider _userProvider;

        public OrderService(IApplicationDbContext applicationDbContext, IValidator<OrderRequestDto> validator, ITaxService taxService, IKdsNotificationService kdsNotificationService, IUserProvider userProvider)
        {
            _context = applicationDbContext;
            _validator = validator;
            _mapper = new OrderMapper();
            _orderItemMapper = new OrderItemMapper();
            _paymentMapper = new PaymentMapper();
            _taxService = taxService;
            _kdsNotification = kdsNotificationService;
            _userProvider = userProvider;
        }

        public async Task<OrderResponseDto> AddItemsToOrder(Guid orderId, List<OrderItemRequestDto> itemsDto)
        {
            var order = await FindOrderByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException("Pedido não encontrado.");

            foreach (var item in itemsDto)
            {
                var newItem = await BuildOrderItemAsync(item);

                newItem.OrderId = orderId;
                newItem.KdsStatus = KdsStatus.Pending;

                _context.OrderItems.Add(newItem);

                order.Items.Add(newItem);
            }

            order.TotalAmount = order.Items.Sum(i => i.UnitPrice * i.Quantity);

            var totalPaid = order.Payments.Sum(p => p.Amount);
            if (order.PaymentStatus == PaymentStatus.Paid && order.TotalAmount > totalPaid)
            {
                order.PaymentStatus = PaymentStatus.Partial;
            }

            if (order.OrderStatus == OrderStatus.ReadyToServe || order.OrderStatus == OrderStatus.Delivered)
            {
                order.OrderStatus = OrderStatus.Preparing;
            }

            await _context.SaveChangesAsync();

            var orderResponseDto = _mapper.ToDto(order);

            await _kdsNotification.NotifyOrderUpdatedAsync(order.TenantId.ToString(), orderResponseDto);

            return orderResponseDto;
        }

        public async Task<OrderResponseDto> AddItemToOrder(Guid orderId, OrderItemRequestDto itemDto)
        {
            var order = await FindOrderByIdAsync(orderId);

            var newItem = await BuildOrderItemAsync(itemDto);

            order.Items.Add(newItem);
            order.TotalAmount += newItem.UnitPrice * newItem.Quantity;

            await _context.SaveChangesAsync();

            var orderResponseDto = _mapper.ToDto(order);
            await _kdsNotification.NotifyOrderUpdatedAsync(order.TenantId.ToString(), orderResponseDto);

            return orderResponseDto;
        }

        public async Task<OrderResponseDto> CheckoutOrder(Guid orderId, CheckoutRequestDto checkoutDto)
        {
            var order = await FindOrderByIdAsync(orderId);
            var employeeId = _userProvider.GetUserId() ?? throw new UnauthorizedAccessException("Usuário não autenticado.");

            if (order.PaymentStatus == PaymentStatus.Paid)
                throw new ArgumentException($"Pedido já pago.");

            decimal totalValue = 0;
            foreach (var paymentDto in checkoutDto.Payments)
            {
                totalValue += paymentDto.Amount;

                var payment = _paymentMapper.ToEntity(paymentDto);

                order.Payments.Add(payment);
                _context.Payments.Add(payment);
            }

            if (totalValue < order.TotalAmount)
                throw new ArgumentException($"Valor pago é insuficiente.");

            order = await CheckoutOrderAsync(order, employeeId);

            await _context.SaveChangesAsync();

            var orderResponse = _mapper.ToDto(order);

            var changeAmount = totalValue - order.TotalAmount;
            orderResponse.ChangeAmount = changeAmount;

            return orderResponse;
        }

        public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(DateTime startDate, DateTime endDate)
        {
            var baseQuery = _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Payments)
                .Where(o => o.PaymentStatus == PaymentStatus.Paid &&
                            o.CreatedAt >= startDate &&
                            o.CreatedAt <= endDate);

            var orders = await baseQuery.ToListAsync();

            var totalOrders = orders.Count;
            var totalRevenue = orders.Sum(o => o.TotalAmount);
            var averageTicket = totalOrders > 0 ? totalRevenue / totalOrders : 0;

            var topItems = orders.SelectMany(o => o.Items)
                .GroupBy(i => i.Product.Name)
                .Select(g => new TopSellingItemDto
                {
                    ProductName = g.Key,
                    QuantitySold = g.Sum(i => i.Quantity)
                })
                .OrderByDescending(x => x.QuantitySold)
                .ToList();

            var cashFlow = orders.SelectMany(o => o.Payments)
                .GroupBy(p => p.Method)
                .Select(g => new CashFlowDto
                {
                    PaymentMethod = g.Key.ToString(),
                    TotalAmount = g.Sum(p => p.Amount)
                })
                .ToList();

            var salesChart = orders
                .GroupBy(o => o.CreatedAt.Hour)
                .Select(g => new SalesChartDto
                {
                    Label = $"{g.Key:00}:00",
                    Value = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Label)
                .ToList();

            return new DashboardMetricsDto
            {
                TotalRevenue = totalRevenue,
                TotalOrders = totalOrders,
                AverageTicket = averageTicket,
                TopSellingItems = topItems,
                CashFlow = cashFlow,
                SalesChart = salesChart
            };
        }

        public async Task<OrderResponseDto> CreateOrder(OrderRequestDto dto)
        {
            var employeeId = _userProvider.GetUserId() ?? throw new UnauthorizedAccessException("Usuário não autenticado.");

            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null)
                throw new Exception("Funcionário não encontrado no banco de dados.");

            var tenantId = employee.TenantId.ToString();

            await _validator.ValidateAndThrowAsync(dto);

            var orderToAdd = _mapper.ToEntity(dto);

            orderToAdd.EmployeeId = employeeId;
            orderToAdd.Items.Clear();

            decimal totalPrice = 0;

            foreach (var item in dto.Items)
            {
                var product = await FindProductByIdAsync(item.ProductId);

                var newItem = await BuildOrderItemAsync(item);
                orderToAdd.Items.Add(newItem);
                totalPrice += (newItem.UnitPrice * newItem.Quantity);
            }

            orderToAdd.TotalAmount = totalPrice;
            orderToAdd.EmployeeId = employeeId;

            decimal totalValue = 0;
            foreach (var paymentDto in dto.Payments)
            {
                totalValue += paymentDto.Amount;
            }

            if (totalValue >= totalPrice)
            {
                orderToAdd = await CheckoutOrderAsync(orderToAdd, employeeId);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.Orders.AddAsync(orderToAdd);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var orderResponseDto = _mapper.ToDto(order.Entity);
                await _kdsNotification.NotifyOrderUpdatedAsync(tenantId, orderResponseDto);

                return orderResponseDto;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<PagedResponse<OrderResponseDto>> GetAllOrders(OrderQueryParameters queryParameters)
        {
            IQueryable<Order> query = _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Items).ThenInclude(i => i.SelectedModifiers)
                .Include(o => o.Payments)
                .Include(o => o.Employee)
                .Include(o => o.PaidBy)
                .AsQueryable();

            if (!string.IsNullOrEmpty(queryParameters.SearchTerm))
            {
                var searchTerm = queryParameters.SearchTerm.ToLower();
                query = query.Where(o => (o.TableNumber != null && o.TableNumber.ToLower().Contains(searchTerm)) || o.Id.ToString().StartsWith(searchTerm));
            }

            if (queryParameters.PaymentStatus.HasValue) query = query.Where(o => o.PaymentStatus == queryParameters.PaymentStatus);
            if (queryParameters.OrderStatus.HasValue) query = query.Where(o => o.OrderStatus == queryParameters.OrderStatus);
            if (queryParameters.StartDate.HasValue) query = query.Where(o => o.CreatedAt >= queryParameters.StartDate.Value);
            if (queryParameters.EndDate.HasValue) query = query.Where(o => o.CreatedAt <= queryParameters.EndDate.Value);

            IOrderedQueryable<Order> orderedQuery = query.ApplySorting(queryParameters.SortBy, queryParameters.IsDescending, (sortBy, descending) => sortBy switch
            {
                "tablenumber" => descending ? query.OrderByDescending(o => o.TableNumber) : query.OrderBy(o => o.TableNumber),
                "id" => descending ? query.OrderByDescending(o => o.Id) : query.OrderBy(o => o.Id),
                "totalamount" => descending ? query.OrderByDescending(o => o.TotalAmount) : query.OrderBy(o => o.TotalAmount),
                "orderstatus" => descending ? query.OrderByDescending(o => o.OrderStatus) : query.OrderBy(o => o.OrderStatus),
                "paymentstatus" => descending ? query.OrderByDescending(o => o.PaymentStatus) : query.OrderBy(o => o.PaymentStatus),
                _ => descending ? query.OrderByDescending(o => o.CreatedAt) : query.OrderBy(o => o.CreatedAt)
            });

            var totalRecords = await orderedQuery.CountAsync();

            var orders = await orderedQuery
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .ToListAsync();

            return new PagedResponse<OrderResponseDto>
            {
                Data = orders.Select(c => _mapper.ToDto(c)),
                PageNumber = queryParameters.PageNumber,
                PageSize = queryParameters.PageSize,
                TotalRecords = totalRecords
            };
        }

        public async Task<OrderResponseDto> GetOrderById(Guid orderId)
        {
            var order = await FindOrderByIdAsync(orderId);
            return _mapper.ToDto(order);
        }

        public async Task<OrderResponseDto> RemoveItemFromOrder(Guid orderId, Guid itemId)
        {
            var order = await FindOrderByIdAsync(orderId);

            if (order.OrderStatus == OrderStatus.Canceled || order.PaymentStatus == PaymentStatus.Paid)
                throw new ArgumentException("Incapaz de remover qualquer item deste pedido.");

            var item = await FindItemByIdAsync(order, itemId);

            _context.OrderItems.Remove(item);
            order.TotalAmount -= (item.UnitPrice * item.Quantity);

            await _context.SaveChangesAsync();
            return _mapper.ToDto(order);
        }

        public async Task<OrderResponseDto> UpdateItemStatus(Guid orderId, Guid itemId, KdsStatus status)
        {
            var order = await FindOrderByIdAsync(orderId);
            var item = await FindItemByIdAsync(order, itemId);

            item.KdsStatus = status;

            bool isAllItemsDone = order.Items.All(i => i.KdsStatus == KdsStatus.Done);

            if (isAllItemsDone && order.OrderStatus != OrderStatus.Canceled && order.OrderStatus != OrderStatus.Delivered)
            {
                order.OrderStatus = OrderStatus.ReadyToServe;
            }
            else if (status == KdsStatus.Preparing && order.OrderStatus == OrderStatus.Open)
            {
                order.OrderStatus = OrderStatus.Preparing;
            }

            await _context.SaveChangesAsync();

            var orderResponseDto = _mapper.ToDto(order);
            await _kdsNotification.NotifyOrderUpdatedAsync(order.TenantId.ToString(), orderResponseDto);

            return orderResponseDto;
        }

        public async Task<IEnumerable<OrderResponseDto>> GetPendingKdsOrdersAsync()
        {
            var orders = await _context.Orders
                            .Include(o => o.Items).ThenInclude(i => i.Product)
                            .Include(o => o.Items).ThenInclude(i => i.SelectedModifiers)
                            .Include(o => o.Employee)
                            .Where(o => o.OrderStatus == OrderStatus.Open || o.OrderStatus == OrderStatus.Preparing)
                            .OrderBy(o => o.CreatedAt)
                            .ToListAsync();

            return orders.Select(o => _mapper.ToDto(o)).ToList();
        }

        public async Task<OrderResponseDto> CancelOrder(Guid orderId)
        {
            var order = await FindOrderByIdAsync(orderId);

            if (order.PaymentStatus == PaymentStatus.Refunded)
                throw new ArgumentException("Não é possível cancelar um pedido estornado.");
            if (order.PaymentStatus == PaymentStatus.Paid)
                throw new ArgumentException("Não é possível cancelar um pedido já pago.");

            order.OrderStatus = OrderStatus.Canceled;
            await _context.SaveChangesAsync();

            var responseDto = _mapper.ToDto(order);
            await _kdsNotification.NotifyOrderCanceledAsync(order.TenantId.ToString(), responseDto);

            return responseDto;
        }

        public async Task<OrderResponseDto> DeliverOrder(Guid orderId)
        {
            var order = await FindOrderByIdAsync(orderId);

            if (order.OrderStatus == OrderStatus.Canceled)
                throw new ArgumentException("Não é possível entregar um pedido cancelado.");

            if (order.PaymentStatus == PaymentStatus.Refunded)
                throw new ArgumentException("Não é possível entregar um pedido estornado.");

            order.OrderStatus = OrderStatus.Delivered;

            await _context.SaveChangesAsync();
            var responseDto = _mapper.ToDto(order);
            await _kdsNotification.NotifyDeliveredItemAsync(order.TenantId.ToString(), responseDto);

            return responseDto;
        }

        private async Task<Order> CheckoutOrderAsync(Order order, Guid paidById)
        {
            order.PaymentStatus = PaymentStatus.Paid;
            order.PaidById = paidById;
            order.ReceiptUrl = await _taxService.GenerateNfceAsync(order);
            return order;
        }

        private async Task<OrderItem> BuildOrderItemAsync(OrderItemRequestDto itemDto)
        {
            var product = await FindProductByIdAsync(itemDto.ProductId);
            var newItem = _orderItemMapper.ToEntity(itemDto);

            newItem.Id = Guid.NewGuid();
            newItem.ProductId = product.Id;
            newItem.Product = product;
            decimal modifiersTotal = 0;

            if (itemDto.SelectedModifierIds != null && itemDto.SelectedModifierIds.Any())
            {
                var modifiers = await _context.ModifierOptions
                    .Where(m => itemDto.SelectedModifierIds.Contains(m.Id))
                    .ToListAsync();

                foreach (var modId in itemDto.SelectedModifierIds)
                {
                    var modifierOption = modifiers.FirstOrDefault(m => m.Id == modId);
                    if (modifierOption == null)
                        throw new ArgumentException("Opção adicional não encontrada.");

                    newItem.SelectedModifiers.Add(new OrderItemModifier
                    {
                        Id = Guid.NewGuid(),
                        Name = modifierOption.Name,
                        AdditionalPrice = modifierOption.AdditionalPrice
                    });

                    modifiersTotal += modifierOption.AdditionalPrice;
                }
            }
            newItem.UnitPrice = product.Price + modifiersTotal;

            return newItem;
        }

        private async Task<Order> FindOrderByIdAsync(Guid orderId)
        {
            var order = await _context.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Items).ThenInclude(i => i.SelectedModifiers)
            .Include(o => o.Payments)
            .Include(o => o.Employee)
            .Include(o => o.PaidBy)
            .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new ArgumentException($"Pedido não encontrado.");
            return order;
        }

        private async Task<Product> FindProductByIdAsync(Guid productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                throw new ArgumentException($"Produto não encontrado.");
            return product;
        }

        private async Task<OrderItem> FindItemByIdAsync(Order order, Guid itemId)
        {
            var item = order.Items.FirstOrDefault(i => i.Id == itemId);
            if (item == null)
                throw new ArgumentException($"Item não encontrado no pedido.");
            return item;
        }
    }
}