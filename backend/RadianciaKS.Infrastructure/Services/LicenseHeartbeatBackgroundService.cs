using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RadianciaKS.Application.Services;
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
                using (var scope = _scopeFactory.CreateScope())
                {
                    var syncService = scope.ServiceProvider.GetRequiredService<ILicenseSyncService>();
                    await syncService.SyncLicenseAsync(stoppingToken);
                }

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
    }
}