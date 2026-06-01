using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Domain.Interfaces;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Context
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        private readonly ITenantProvider _tenantProvider;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider) : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<ModifierGroup> ModifierGroups { get; set; }
        public DbSet<ModifierOption> ModifierOptions { get; set; }
        public DbSet<OrderItemModifier> OrderItemModifiers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {

                bool hasActiveProperty = entity.FindProperty("Active") is not null;
                bool hasTenant = typeof(IMustHaveTenant).IsAssignableFrom(entity.ClrType);

                if (hasActiveProperty || hasTenant)
                {
                    var method = typeof(ApplicationDbContext)
                        .GetMethod(nameof(ConfigureGlobalFilters), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                        .MakeGenericMethod(entity.ClrType);

                    method?.Invoke(this, new object[] { modelBuilder, hasTenant, hasActiveProperty });
                }
            }
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Guid tenantId = _tenantProvider.GetTenantId();

            var addedEntries = ChangeTracker.Entries().Where(e => e.State == EntityState.Added && e.Entity is IMustHaveTenant);
            foreach (var entry in addedEntries)
            {
                if (entry.Entity is IMustHaveTenant entryTenant && entryTenant.TenantId == Guid.Empty)
                {
                    entryTenant.TenantId = tenantId;
                }
            }

            var deletedEntries = ChangeTracker.Entries().Where(e => e.State == EntityState.Deleted && e.Metadata.FindProperty("Active") != null);

            foreach (var entry in deletedEntries)
            {
                entry.State = EntityState.Modified;
                entry.Property("Active").CurrentValue = false;
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        private void ConfigureGlobalFilters<TEntity>(ModelBuilder modelBuilder, bool hasTenant, bool hasSoftDelete) where TEntity : class
        {
            if (hasTenant && hasSoftDelete)
            {
                modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
                    ((IMustHaveTenant)e).TenantId == _tenantProvider.GetTenantId() &&
                    EF.Property<bool>(e, "Active") == true);
            }
            else if (hasTenant)
            {
                modelBuilder.Entity<TEntity>().HasQueryFilter(e => ((IMustHaveTenant)e).TenantId == _tenantProvider.GetTenantId());
            }
            else if (hasSoftDelete)
            {
                modelBuilder.Entity<TEntity>().HasQueryFilter(e => EF.Property<bool>(e, "Active") == true);
            }
        }
    }
}