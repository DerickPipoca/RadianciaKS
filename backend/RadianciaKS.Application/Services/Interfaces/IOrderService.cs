using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrder(OrderRequestDto dto);
        Task<IEnumerable<OrderResponseDto>> GetAllOrders();
        Task<OrderResponseDto> AddItemToOrder(Guid orderId, OrderItemRequestDto itemDto);
        Task<OrderResponseDto> CheckoutOrder(Guid orderId, CheckoutRequestDto checkoutDto);
        Task<OrderResponseDto> UpdateItemStatusAsync(Guid orderId, Guid itemId, KdsStatus status);
    }
}