using RadianciaKS.Application.DTOs.Licensing;

namespace RadianciaKS.Application.Services.Interfaces
{
    public interface IKeygenService
    {
        Task<KeygenValidationResult> ValidateKeyAsync(string licenseKey, string? fingerprint = null, CancellationToken ct = default);
        Task<bool> RegisterMachineAsync(string licenseKey, string licenseId, string fingerprint, string machineName, CancellationToken ct = default);
    }
}