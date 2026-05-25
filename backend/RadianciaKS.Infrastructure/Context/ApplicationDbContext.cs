using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Domain.Interfaces;

namespace RadianciaKS.Infrastructure.Context
{
    public class ApplicationDbContext : DbContext
    {
        private readonly ITenantProvider _tenantProvider;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider) : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        //TODO: DbSets

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(IMustHaveTenant).IsAssignableFrom(entity.ClrType))
                {
                    var method = typeof(ApplicationDbContext)
                        .GetMethod(nameof(ConfigureTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                        .MakeGenericMethod(entity.ClrType);

                    method?.Invoke(this, new object[] { modelBuilder });
                }
            }
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {


            Guid tenantId = _tenantProvider.GetTenantId();
            var entries = ChangeTracker.Entries().Where(e => e.State == EntityState.Added && e.Entity is IMustHaveTenant);

            foreach (var entry in entries)
            {
                if (entry.Entity is IMustHaveTenant entryTenant)
                {
                    entryTenant.TenantId = tenantId;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        private void ConfigureTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, IMustHaveTenant
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == _tenantProvider.GetTenantId());
        }
    }
}