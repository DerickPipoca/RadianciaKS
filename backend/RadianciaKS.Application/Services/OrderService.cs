using FluentValidation;
using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Mappers;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Application.Services
{
    public class OrderService : IOrderService
    {

        private readonly IApplicationDbContext _context;
        private readonly IValidator<OrderRequestDto> _validator;
        private readonly OrderMapper _mapper;

        public OrderService(IApplicationDbContext applicationDbContext, IValidator<OrderRequestDto> validator)
        {
            _context = applicationDbContext;
            _validator = validator;
            _mapper = new OrderMapper();
        }

        public async Task<OrderResponseDto> CreateOrder(OrderRequestDto dto)
        {
            await _validator.ValidateAndThrowAsync(dto);
            var orderToAdd = _mapper.ToEntity(dto);

            decimal totalPrice = 0;
            foreach (var item in orderToAdd.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                    throw new ArgumentException($"Produto não encontrado");

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
    }
}