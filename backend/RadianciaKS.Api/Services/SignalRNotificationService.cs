using Microsoft.AspNetCore.SignalR;
using RadianciaKS.Api.Hubs;
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

        public async Task NotifyItemReadyAsync(string tenantId, object item)
        {
            await _hubContext.Clients.Group(tenantId).SendAsync("OnItemReady", item);
        }

        public async Task NotifyNewItemAsync(string tenantId, object item)
        {
            await _hubContext.Clients.Group(tenantId).SendAsync("OnNewOrder", item);
        }
    }
}