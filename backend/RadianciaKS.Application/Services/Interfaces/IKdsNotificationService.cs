using RadianciaKS.Application.DTOs.Order;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IKdsNotificationService
    {
        Task NotifyOrderUpdatedAsync(string tenantId, OrderResponseDto order);
        Task NotifyDeliveredItemAsync(string tenantId, object item);
    }
}