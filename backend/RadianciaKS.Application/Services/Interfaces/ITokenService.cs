using RadianciaKS.Domain.Models;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(Employee employee);
    }
}