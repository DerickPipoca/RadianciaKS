using RadianciaKS.Application.DTOs.Licensing;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IKeygenService
    {
        Task<KeygenValidationResult> ValidateKeyAsync(string licenseKey, CancellationToken ct = default);
    }
}