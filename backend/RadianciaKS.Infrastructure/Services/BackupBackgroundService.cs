using System.Diagnostics;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Infrastructure.Configuration;

namespace RadianciaKS.Infrastructure.Services
{
    public class BackupBackgroundService : BackgroundService
    {
        private readonly IBackupQueue _backupQueue;
        private readonly IConfiguration _configuration;
        private readonly CloudflareR2Settings _r2Settings;
        private readonly ILogger<BackupBackgroundService> _logger;
        private const string BackupDirectory = "/app/backups";
        private const int LocalRetentionDays = 7;

        public BackupBackgroundService(
                IBackupQueue backupQueue,
                IConfiguration configuration,
                IOptions<CloudflareR2Settings> r2Options,
                ILogger<BackupBackgroundService> logger)
        {
            _backupQueue = backupQueue;
            _configuration = configuration;
            _r2Settings = r2Options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Serviço de Backup iniciado e monitorando fechamentos de caixa.");

            await foreach (var tenantId in _backupQueue.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessBackupAsync(tenantId, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha durante o backup do Tenant {TenantId}", tenantId);
                }
            }
        }

        private async Task ProcessBackupAsync(Guid tenantId, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(BackupDirectory);

            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var fileName = $"backup_{tenantId}_{timestamp}.dump";
            var localFilePath = Path.Combine(BackupDirectory, fileName);

            _logger.LogInformation("Gerando dump binário PostgreSQL: {FileName}", fileName);
            await GeneratePgDumpAsync(localFilePath, cancellationToken);

            _logger.LogInformation("Enviando dump para o Cloudflare R2: {FileName}", fileName);
            await UploadToR2Async(localFilePath, fileName, tenantId, cancellationToken);

            _logger.LogInformation("Executando expurgo de backups locais com mais de {Days} dias.", LocalRetentionDays);
            CleanOldLocalBackups();
        }

        private async Task GeneratePgDumpAsync(string outputFilePath, CancellationToken cancellationToken)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            var builder = new NpgsqlConnectionStringBuilder(connectionString);

            var startInfo = new ProcessStartInfo
            {
                FileName = "pg_dump",
                Arguments = $"-h {builder.Host} -p {builder.Port} -U {builder.Username} -d {builder.Database} -F c -b -v -f \"{outputFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            startInfo.Environment["PGPASSWORD"] = builder.Password;

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var errorOutputTask = process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                var errorOutput = await errorOutputTask;
                throw new InvalidOperationException($"pg_dump encerrou com erro (Código {process.ExitCode}): {errorOutput}");
            }
        }

        private async Task UploadToR2Async(string filePath, string fileName, Guid tenantId, CancellationToken cancellationToken)
        {
            var credentials = new BasicAWSCredentials(_r2Settings.AccessKeyId, _r2Settings.SecretAccessKey);
            var config = new AmazonS3Config
            {
                ServiceURL = _r2Settings.Endpoint,
                ForcePathStyle = true
            };

            using var client = new AmazonS3Client(credentials, config);

            var putRequest = new PutObjectRequest
            {
                BucketName = _r2Settings.BucketName,
                Key = $"backups/{tenantId}/{fileName}",
                FilePath = filePath,
                DisablePayloadSigning = true
            };

            await client.PutObjectAsync(putRequest, cancellationToken);
            _logger.LogInformation("Backup enviado com sucesso: backups/{TenantId}/{FileName}", tenantId, fileName);
        }

        private void CleanOldLocalBackups()
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-LocalRetentionDays);
            var files = Directory.GetFiles(BackupDirectory, "*.dump");

            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.CreationTimeUtc < cutoffDate)
                {
                    try
                    {
                        fileInfo.Delete();
                        _logger.LogInformation("Backup local expirado removido: {FileName}", fileInfo.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Não foi possível remover o arquivo local antigo: {FileName}", fileInfo.Name);
                    }
                }
            }
        }
    }
}