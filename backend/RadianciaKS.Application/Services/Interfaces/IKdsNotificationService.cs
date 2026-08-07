namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IKdsNotificationService
    {
        Task NotifyNewItemAsync(string tenantId, object item);
        Task NotifyItemReadyAsync(string tenantId, object item);
        Task NotifyDeliveredItemAsync(string tenantId, object item);
    }
}