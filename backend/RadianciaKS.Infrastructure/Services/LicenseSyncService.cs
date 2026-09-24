using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadianciaKS.Application.DTOs.Licensing;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;
using RadianciaKS.Infrastructure.Licensing.Configuration;

namespace RadianciaKS.Infrastructure.Services
{
    public class LicenseSyncService : ILicenseSyncService
    {
        private readonly IApplicationDbContext _dbContext;
        private readonly IKeygenService _keygenService;
        private readonly KeygenSettings _settings;
        private readonly ILogger<LicenseSyncService> _logger;

        public LicenseSyncService(
            IApplicationDbContext dbContext,
            IKeygenService keygenService,
            IOptions<KeygenSettings> settings,
            ILogger<LicenseSyncService> logger)
        {
            _dbContext = dbContext;
            _keygenService = keygenService;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SyncLicenseAsync(CancellationToken ct = default)
        {
            try
            {
                var license = await _dbContext.SystemLicenses.FirstOrDefaultAsync(ct);
                if (license == null)
                {
                    _logger.LogWarning("Nenhuma licença registrada na tabela SystemLicenses.");
                    return;
                }

                await EnsureFingerprintAsync(license, ct);

                var now = DateTimeOffset.UtcNow;
                var result = await _keygenService.ValidateKeyAsync(license.LicenseKey, license.MachineFingerprint, ct);

                result = await TryRegisterMachineIfRequiredAsync(license, result, ct);

                if (result.IsOnline)
                {
                    ApplyOnlineValidation(license, result, now);
                }
                else
                {
                    ApplyOfflineContingency(license, now);
                }

                await _dbContext.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[KEYGEN] Erro ao executar sincronização de licença.");
            }
        }

        private async Task EnsureFingerprintAsync(SystemLicense license, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(license.MachineFingerprint)) return;

            license.MachineFingerprint = Guid.NewGuid().ToString("N");
            await _dbContext.SaveChangesAsync(ct);
        }

        private async Task<KeygenValidationResult> TryRegisterMachineIfRequiredAsync(
    SystemLicense license,
    KeygenValidationResult result,
    CancellationToken ct)
        {
            if (!result.IsOnline || string.IsNullOrWhiteSpace(result.LicenseId))
            {
                return result;
            }

            var requiresMachineRegistration = result.Code is "NO_MACHINE" or "NO_MACHINES" or "FINGERPRINT_SCOPE_MISMATCH";
            if (!requiresMachineRegistration)
            {
                return result;
            }

            var fingerprint = license.MachineFingerprint;
            if (string.IsNullOrWhiteSpace(fingerprint))
            {
                _logger.LogWarning("[KEYGEN] Não foi possível registar a máquina: Fingerprint ausente.");
                return result;
            }

            _logger.LogWarning("[KEYGEN] Licença exige máquina ({Code}). Registrando fingerprint {Fingerprint} para licença {LicenseId}...",
                result.Code, fingerprint, result.LicenseId);

            var registered = await _keygenService.RegisterMachineAsync(
                license.LicenseKey,
                result.LicenseId,
                fingerprint,
                _settings.MachineName,
                ct);

            if (!registered) return result;

            return await _keygenService.ValidateKeyAsync(license.LicenseKey, fingerprint, ct);
        }

        private void ApplyOnlineValidation(SystemLicense license, KeygenValidationResult result, DateTimeOffset now)
        {
            license.LastValidatedAt = now;
            license.UpdatedAt = now;

            if (result.Expiry.HasValue)
            {
                license.ExpiresAt = result.Expiry.Value;
            }

            license.Status = MapLicenseStatus(result.Code);
            TouchSystemTime(license, now);

            _logger.LogInformation("Licença sincronizada online. Estado: {Status} | Expira em: {ExpiresAt}",
                license.Status, license.ExpiresAt);
        }

        private void ApplyOfflineContingency(SystemLicense license, DateTimeOffset now)
        {
            TouchSystemTime(license, now);

            _logger.LogWarning("Keygen inacessível. Modo de contingência local ativo até {ExpiresAt}. Estado: {Status}",
                license.ExpiresAt, license.Status);
        }

        private static LicenseStatus MapLicenseStatus(string code) => code switch
        {
            "VALID" => LicenseStatus.ACTIVE,
            "SUSPENDED" => LicenseStatus.SUSPENDED,
            "EXPIRED" => LicenseStatus.EXPIRED,
            _ => LicenseStatus.UNVALIDATED
        };

        private static void TouchSystemTime(SystemLicense license, DateTimeOffset now)
        {
            if (now > license.LastKnownSystemTime)
            {
                license.LastKnownSystemTime = now;
            }
        }
    }
}