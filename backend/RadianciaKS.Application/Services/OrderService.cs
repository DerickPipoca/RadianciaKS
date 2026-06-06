using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.DTOs.Order;
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

        public OrderService(IApplicationDbContext applicationDbContext, IValidator<OrderRequestDto> validator, ITaxService taxService, IKdsNotificationService kdsNotificationService)
        {
            _context = applicationDbContext;
            _validator = validator;
            _mapper = new OrderMapper();
            _orderItemMapper = new OrderItemMapper();
            _paymentMapper = new PaymentMapper();
            _taxService = taxService;
            _kdsNotification = kdsNotificationService;
        }

        public async Task<OrderResponseDto> AddItemToOrder(Guid orderId, OrderItemRequestDto itemDto)
        {
            var order = await FindOrderByIdAsync(orderId);

            var newItem = await BuildOrderItemAsync(itemDto);

            order.Items.Add(newItem);
            order.TotalAmount += newItem.UnitPrice * newItem.Quantity;

            var tenantId = order.TenantId.ToString();
            var itemResponse = _orderItemMapper.ToDto(newItem);
            await _kdsNotification.NotifyNewItemAsync(tenantId, itemResponse);

            await _context.SaveChangesAsync();
            return _mapper.ToDto(order);
        }

        public async Task<OrderResponseDto> CheckoutOrder(Guid orderId, CheckoutRequestDto checkoutDto)
        {
            var order = await FindOrderByIdAsync(orderId);

            if (order.OrderStatus == OrderStatus.Paid)
                throw new ArgumentException($"Pedido já encerrado.");

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

            order.OrderStatus = OrderStatus.Paid;

            order.ReceiptUrl = await _taxService.GenerateNfceAsync(order);

            await _context.SaveChangesAsync();

            var orderResponse = _mapper.ToDto(order);

            var changeAmount = totalValue - order.TotalAmount;
            orderResponse.ChangeAmount = changeAmount;

            return orderResponse;
        }

        public async Task<OrderResponseDto> CreateOrder(OrderRequestDto dto)
        {
            await _validator.ValidateAndThrowAsync(dto);

            var orderToAdd = _mapper.ToEntity(dto);
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

            var order = _context.Orders.Add(orderToAdd);
            await _context.SaveChangesAsync();

            var tenantId = order.Entity.TenantId.ToString();
            
            foreach (var item in order.Entity.Items)
            {
                var itemResponse = _orderItemMapper.ToDto(item);
                await _kdsNotification.NotifyNewItemAsync(tenantId, itemResponse);
            }

            return _mapper.ToDto(order.Entity);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.Items).ThenInclude(i => i.SelectedModifiers)
                .Include(o => o.Payments)
                .ToListAsync();
            return orders.Select(c => _mapper.ToDto(c));
        }

        public async Task<OrderResponseDto> GetOrderById(Guid orderId)
        {
            var order = await FindOrderByIdAsync(orderId);
            return _mapper.ToDto(order);
        }

        public async Task<OrderResponseDto> RemoveItemFromOrder(Guid orderId, Guid itemId)
        {
            var order = await FindOrderByIdAsync(orderId);

            if (order.OrderStatus == OrderStatus.Canceled || order.OrderStatus == OrderStatus.Paid)
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

            if (isAllItemsDone && order.OrderStatus != OrderStatus.Paid)
            {
                order.OrderStatus = OrderStatus.ReadyToServe;
            }

            await _context.SaveChangesAsync();

            var itemResponse = _orderItemMapper.ToDto(item);
            await _kdsNotification.NotifyItemReadyAsync(order.TenantId.ToString(), itemResponse);

            return _mapper.ToDto(order);
        }

        public async Task<OrderResponseDto> CancelOrder(Guid orderId)
        {
            var order = await FindOrderByIdAsync(orderId);
            if (order.OrderStatus == OrderStatus.Paid)
                throw new ArgumentException("Não é possível cancelar um pedido já pago.");

            order.OrderStatus = OrderStatus.Canceled;

            await _context.SaveChangesAsync();
            return _mapper.ToDto(order);
        }

        private async Task<OrderItem> BuildOrderItemAsync(OrderItemRequestDto itemDto)
        {
            var product = await FindProductByIdAsync(itemDto.ProductId);
            var newItem = _orderItemMapper.ToEntity(itemDto);

            newItem.Product = product;
            decimal modifiersTotal = 0;

            if (itemDto.SelectedModifierIds != null && itemDto.SelectedModifierIds.Count != 0)
            {
                foreach (var modId in itemDto.SelectedModifierIds)
                {
                    var modifierOption = await _context.ModifierOptions.FindAsync(modId);
                    if (modifierOption == null)
                        throw new ArgumentException("Opção adicional não encontrada.");

                    newItem.SelectedModifiers.Add(new OrderItemModifier
                    {
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