using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Domain.Enums;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IKdsNotificationService
    {
        Task NotifyOrderUpdatedAsync(string tenantId, OrderResponseDto order);
        Task UpdateCashShiftStatusAsync(string tenantId, CashShiftStatus status);
        Task NotifyDeliveredItemAsync(string tenantId, object item);
        Task NotifyOrderCanceledAsync(string tenantId, OrderResponseDto order);
    }
}