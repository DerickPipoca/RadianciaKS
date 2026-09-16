using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Domain.Enums;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IApplicationDbContext context, IConfiguration configuration, ILogger logger)
        {
            try
            {
                if ((await context.Database.GetPendingMigrationsAsync()).Any())
                {
                    logger.LogInformation("[RADIÂNCIA_KS] Aplicando migrações pendentes...");
                    await context.Database.MigrateAsync();
                }

                var rawTenantId = configuration["TENANT_ID"]
                    ?? Environment.GetEnvironmentVariable("TENANT_ID");

                if (rawTenantId == null)
                    throw new ArgumentException("Variavel de ambiente 'TENANT_ID' não definida...");

                var seedTenantId = Guid.Parse(rawTenantId);

                if (!await context.StoreSettings.IgnoreQueryFilters().AnyAsync(s => s.TenantId == seedTenantId))
                {
                    logger.LogInformation("[RADIÂNCIA_KS] Criando configurações para o Tenant {TenantId}...", seedTenantId);
                    await context.StoreSettings.AddAsync(new StoreSettings
                    {
                        TenantId = seedTenantId,
                        StoreName = "Radiância Sistemas - Unidade",
                        CNPJ = "00000000000100",
                        ServiceCharge = 0.0m
                    });
                }

                if (!await context.Employees.IgnoreQueryFilters().AnyAsync(e => e.TenantId == seedTenantId && e.Role == EmployeeRole.Admin))
                {
                    logger.LogInformation("[RADIÂNCIA_KS] Criando Administrador primordial para o Tenant {TenantId}...", seedTenantId);
                    await context.Employees.AddAsync(new Employee
                    {
                        TenantId = seedTenantId,
                        Name = "Administrador",
                        CPF = configuration["ADMIN_INITIAL_CPF"]!,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(configuration["ADMIN_INITIAL_PASSWORD"]),
                        Role = EmployeeRole.Admin
                    });
                }

                await context.SaveChangesAsync();
                logger.LogInformation("[RADIÂNCIA_KS] Seeding concluído para o Tenant {TenantId}!", seedTenantId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[RADIÂNCIA_KS] Erro ao popular banco de dados.");
                throw;
            }
        }
    }
}