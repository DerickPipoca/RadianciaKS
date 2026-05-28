using Microsoft.AspNetCore.SignalR;

namespace RadianciaKS.Api.Hubs
{
    public class KdsHub : Hub
    {
        public async Task JoinKitchenGroup(string tenantId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, tenantId);
        }

        public async Task LeaveKitchenGroup(string tenantId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, tenantId);
        }
    }
}