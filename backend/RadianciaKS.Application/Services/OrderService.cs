using System.Runtime.CompilerServices;
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

        public OrderService(IApplicationDbContext applicationDbContext, IValidator<OrderRequestDto> validator)
        {
            _context = applicationDbContext;
            _validator = validator;
            _mapper = new OrderMapper();
            _orderItemMapper = new OrderItemMapper();
            _paymentMapper = new PaymentMapper();
        }

        public async Task<OrderResponseDto> AddItemToOrder(Guid orderId, OrderItemRequestDto itemDto)
        {
            var order = await FindOrderByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException($"Operação não encontrada.");

            var product = await FindProductByIdAsync(itemDto.ProductId);
            if (product == null)
                throw new ArgumentException($"Produto não encontrado.");

            var newItem = _orderItemMapper.ToEntity(itemDto);

            newItem.UnitPrice = product.Price;
            newItem.Product = product;

            order.Items.Add(newItem);
            order.TotalAmount += newItem.UnitPrice * newItem.Quantity;

            await _context.SaveChangesAsync();
            return _mapper.ToDto(order);
        }

        public async Task<OrderResponseDto> CheckoutOrder(Guid orderId, CheckoutRequestDto checkoutDto)
        {
            var order = await FindOrderByIdAsync(orderId);
            if (order == null)
                throw new ArgumentException($"Operação não encontrada.");

            if (order.OrderStatus == OrderStatus.Paid)
                throw new ArgumentException($"Pedido já encerrado.");

            decimal totalValue = 0;
            foreach (var payment in checkoutDto.Payments)
            {
                totalValue += payment.Amount;
                order.Payments.Add(_paymentMapper.ToEntity(payment));
            }
            if (totalValue < order.TotalAmount)
                throw new ArgumentException($"Valor pago é insuficiente.");

            order.OrderStatus = OrderStatus.Paid;
            await _context.SaveChangesAsync();
            return _mapper.ToDto(order);
        }

        public async Task<OrderResponseDto> CreateOrder(OrderRequestDto dto)
        {
            await _validator.ValidateAndThrowAsync(dto);
            var orderToAdd = _mapper.ToEntity(dto);

            decimal totalPrice = 0;
            foreach (var item in orderToAdd.Items)
            {
                var product = await FindProductByIdAsync(item.ProductId);
                if (product == null)
                    throw new ArgumentException($"Produto não encontrado.");

                item.UnitPrice = product.Price;
                totalPrice += product.Price * item.Quantity;
                item.Product = product;
            }

            orderToAdd.TotalAmount = totalPrice;

            var order = _context.Orders.Add(orderToAdd);
            await _context.SaveChangesAsync();
            return _mapper.ToDto(order.Entity);
        }

        public async Task<IEnumerable<OrderResponseDto>> GetAllOrders()
        {
            var orders = await _context.Orders.Include(o => o.Items).ThenInclude(i => i.Product).Include(o => o.Payments).ToListAsync();
            return orders.Select(c => _mapper.ToDto(c));
        }

        private async Task<Order?> FindOrderByIdAsync(Guid orderId)
        {
            return await _context.Orders.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == orderId);
        }

        private async Task<Product?> FindProductByIdAsync(Guid productId)
        {
            return await _context.Products.FindAsync(productId);
        }
    }
}