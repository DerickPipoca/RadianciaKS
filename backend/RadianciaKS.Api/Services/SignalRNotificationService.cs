using Microsoft.AspNetCore.SignalR;
using RadianciaKS.Api.Hubs;
using RadianciaKS.Application.DTOs.Order;
using RadianciaKS.Application.Services.Interfaces;

namespace RadianciaKS.Api.Services
{
    public class SignalRNotificationService : IKdsNotificationService
    {
        private readonly IHubContext<KdsHub> _hubContext;

        public SignalRNotificationService(IHubContext<KdsHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyOrderUpdatedAsync(string tenantId, OrderResponseDto order)
        {
            await _hubContext.Clients.Group(tenantId).SendAsync("OnOrderUpdated", order);
        }

        public async Task NotifyDeliveredItemAsync(string tenantId, object item)
        {
            await _hubContext.Clients.Group(tenantId).SendAsync("OnItemDelivered", item);
        }
    }
}