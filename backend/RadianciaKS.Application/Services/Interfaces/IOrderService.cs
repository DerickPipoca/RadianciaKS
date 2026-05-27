using RadianciaKS.Application.DTOs.Order;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrder(OrderRequestDto dto);
        Task<IEnumerable<OrderResponseDto>> GetAllOrders();
    }
}