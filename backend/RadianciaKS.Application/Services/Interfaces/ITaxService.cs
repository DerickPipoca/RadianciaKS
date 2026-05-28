using RadianciaKS.Domain.Models;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface ITaxService
    {
        Task<string> GenerateNfceAsync(Order order);
    }
}