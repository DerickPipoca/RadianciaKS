using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrder(OrderRequestDto dto);
        Task<IEnumerable<OrderResponseDto>> GetAllOrders();
        Task<OrderResponseDto> GetOrderById(Guid orderId);
        Task<OrderResponseDto> AddItemToOrder(Guid orderId, OrderItemRequestDto itemDto);
        Task<OrderResponseDto> CheckoutOrder(Guid orderId, CheckoutRequestDto checkoutDto);
        Task<OrderResponseDto> UpdateItemStatus(Guid orderId, Guid itemId, KdsStatus status);
        Task<OrderResponseDto> RemoveItemFromOrder(Guid orderId, Guid itemId);
        Task<OrderResponseDto> CancelOrder(Guid orderId);
    }
}