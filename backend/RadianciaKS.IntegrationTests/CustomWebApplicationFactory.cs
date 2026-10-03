using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;
using RadianciaKS.Infrastructure.Context;
using RadianciaKS.Infrastructure.Services;

namespace RadianciaKS.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = Guid.NewGuid().ToString();

        // Constante pública para reutilizar nos asserts dos testes
        public const string DefaultCloudApiUrl = "https://test-ks.radianciasistemas.com.br/api";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // 1. Injeta as configurações em memória no IConfiguration
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TENANT_ID"] = TestTenantProvider.DefaultTenantId.ToString(),
                    ["CLOUD_API_URL"] = DefaultCloudApiUrl
                });
            });

            // 2. Configura serviços e banco em memória
            builder.ConfigureServices(services =>
            {
                var dbDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (dbDescriptor != null)
                    services.Remove(dbDescriptor);

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });

                services.RemoveAll<ITenantProvider>();
                services.AddScoped<ITenantProvider, TestTenantProvider>();

                var heartbeatWorkerDescriptor = services.SingleOrDefault(
                    d => d.ImplementationType == typeof(LicenseHeartbeatBackgroundService));

                if (heartbeatWorkerDescriptor != null)
                    services.Remove(heartbeatWorkerDescriptor);
            });

            builder.UseEnvironment("Testing");
        }

        public async Task ResetDatabaseAsync()
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            context.SystemLicenses.Add(new SystemLicense
            {
                Id = Guid.NewGuid(),
                LicenseKey = "TEST-LICENSE-KEY",
                Status = LicenseStatus.ACTIVE,
                ExpiresAt = DateTimeOffset.UtcNow.AddYears(1),
                LastValidatedAt = DateTimeOffset.UtcNow,
                LastKnownSystemTime = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            });

            await context.SaveChangesAsync();
        }
    }
}