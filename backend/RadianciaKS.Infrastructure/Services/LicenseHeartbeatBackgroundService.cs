using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Application.Services.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Infrastructure.Licensing.Configuration;

namespace RadianciaKS.Infrastructure.Services
{
    public class LicenseHeartbeatBackgroundService : BackgroundService
    {

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LicenseHeartbeatBackgroundService> _logger;
        private readonly KeygenSettings _settings;

        public LicenseHeartbeatBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<KeygenSettings> settings,
            ILogger<LicenseHeartbeatBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _settings = settings.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Serviço de Heartbeat de Licenciamento iniciado.");

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromHours(_settings.HeartbeatHours));

            while (!stoppingToken.IsCancellationRequested)
            {
                await ExecuteHeartbeatCycleAsync(stoppingToken);

                try
                {
                    await timer.WaitForNextTickAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task ExecuteHeartbeatCycleAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var keygenService = scope.ServiceProvider.GetRequiredService<IKeygenService>();

                var license = await dbContext.SystemLicenses.FirstOrDefaultAsync(ct);

                if (license == null)
                {
                    _logger.LogWarning("Nenhuma licença registada na tabela SystemLicenses.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(license.MachineFingerprint))
                {
                    license.MachineFingerprint = Guid.NewGuid().ToString("N");
                    await dbContext.SaveChangesAsync(ct);
                }

                var now = DateTimeOffset.UtcNow;
                var result = await keygenService.ValidateKeyAsync(license.LicenseKey, license.MachineFingerprint, ct);

                if (result.IsOnline &&
                        (result.Code == "NO_MACHINE" ||
                        result.Code == "NO_MACHINES" ||
                        result.Code == "FINGERPRINT_SCOPE_MISMATCH"))
                {
                    _logger.LogWarning("[KEYGEN] Licença exige máquina ({Code}). Registrando fingerprint {Fingerprint}...",
                        result.Code, license.MachineFingerprint);

                    var registered = await keygenService.RegisterMachineAsync(
                        license.LicenseKey,
                        license.MachineFingerprint,
                        "Servidor Local Radiância KS",
                        ct);

                    if (registered)
                    {
                        result = await keygenService.ValidateKeyAsync(license.LicenseKey, license.MachineFingerprint, ct);
                    }
                }

                if (result.IsOnline)
                {
                    license.LastValidatedAt = now;
                    license.UpdatedAt = now;

                    if (result.Expiry.HasValue)
                    {
                        license.ExpiresAt = result.Expiry.Value;
                    }

                    license.Status = result.Code switch
                    {
                        "VALID" => LicenseStatus.ACTIVE,
                        "SUSPENDED" => LicenseStatus.SUSPENDED,
                        "EXPIRED" => LicenseStatus.EXPIRED,
                        _ => LicenseStatus.UNVALIDATED
                    };

                    if (now > license.LastKnownSystemTime)
                    {
                        license.LastKnownSystemTime = now;
                    }

                    _logger.LogInformation("Licença sincronizada online. Estado: {Status} | Expira em: {ExpiresAt}", license.Status, license.ExpiresAt);
                }
                else
                {
                    if (now > license.LastKnownSystemTime)
                    {
                        license.LastKnownSystemTime = now;
                    }

                    _logger.LogWarning("Keygen inacessível. Modo de contingência local ativo até {ExpiresAt}. Estado: {Status}",
                        license.ExpiresAt, license.Status);
                }

                await dbContext.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[KEYGEN] Erro ao executar ciclo de heartbeat de licença.");
            }
        }
    }
}