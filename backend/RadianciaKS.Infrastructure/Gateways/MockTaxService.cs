using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Gateways
{
    public class MockTaxService : ITaxService
    {
        public async Task<string> GenerateNfceAsync(Order order)
        {
            await Task.Delay(1500);
            return $"https://sandbox.radianciaks.com/nfce/{order.Id}/nfce.pdf";
        }
    }
}