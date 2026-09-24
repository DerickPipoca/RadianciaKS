namespace RadianciaKS.Application.Services
{
    public interface ILicenseSyncService
    {
        Task SyncLicenseAsync(CancellationToken ct = default);
    }
}