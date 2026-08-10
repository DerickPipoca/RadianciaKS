using RadianciaKS.Application.DTOs;
using RadianciaKS.Application.DTOs.DashboardMetrics;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrder(OrderRequestDto dto);
        Task<PagedResponse<OrderResponseDto>> GetAllOrders(OrderQueryParameters queryParameters);
        Task<IEnumerable<OrderResponseDto>> GetPendingKdsOrdersAsync();
        Task<DashboardMetricsDto> GetDashboardMetricsAsync(DateTime startDate, DateTime endDate);
        Task<OrderResponseDto> GetOrderById(Guid orderId);
        Task<OrderResponseDto> AddItemToOrder(Guid orderId, OrderItemRequestDto itemDto);
        Task<OrderResponseDto> CheckoutOrder(Guid orderId, CheckoutRequestDto checkoutDto);
        Task<OrderResponseDto> UpdateItemStatus(Guid orderId, Guid itemId, KdsStatus status);
        Task<OrderResponseDto> RemoveItemFromOrder(Guid orderId, Guid itemId);
        Task<OrderResponseDto> DeliverOrder(Guid orderId);
        Task<OrderResponseDto> CancelOrder(Guid orderId);
    }
}