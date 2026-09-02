using Microsoft.EntityFrameworkCore;
using RadianciaKS.Application.Interfaces;
using RadianciaKS.Domain.Interfaces;
using RadianciaKS.Domain.Models;
using RadianciaKS.Infrastructure.Data.Auditing;

namespace RadianciaKS.Infrastructure.Context
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly IUserProvider _userProvider;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider, IUserProvider userProvider) : base(options)
        {
            _tenantProvider = tenantProvider;
            _userProvider = userProvider;
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<PromotionModifier> PromotionModifiers { get; set; }
        public DbSet<CashShift> CashShifts { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<ModifierGroup> ModifierGroups { get; set; }
        public DbSet<ModifierOption> ModifierOptions { get; set; }
        public DbSet<StoreSettings> StoreSettings { get; set; }
        public DbSet<OrderItemModifier> OrderItemModifiers { get; set; }

        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("AuditLogs");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.OldValues).HasColumnType("jsonb");
                entity.Property(e => e.NewValues).HasColumnType("jsonb");
            });

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

            OnBeforeSaveChanges();

            var addedAuditLogs = ChangeTracker.Entries().Where(e => e.State == EntityState.Added && e.Entity is IMustHaveTenant);
            foreach (var entry in addedAuditLogs)
            {
                if (entry.Entity is IMustHaveTenant auditTenant && auditTenant.TenantId == Guid.Empty)
                {
                    auditTenant.TenantId = tenantId;
                }
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

        private void OnBeforeSaveChanges()
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();

            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var auditEntry = new AuditEntry(entry)
                {
                    TableName = entry.Metadata.GetTableName() ?? entry.Metadata.ClrType.Name,
                    UserId = _userProvider.GetUserId()
                };

                auditEntries.Add(auditEntry);

                foreach (var property in entry.Properties)
                {
                    if (property.IsTemporary) continue;

                    string propertyName = property.Metadata.Name;

                    switch (entry.State)
                    {
                        case EntityState.Added:
                            auditEntry.Action = "Create";
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                            break;

                        case EntityState.Deleted:
                            auditEntry.Action = "Delete";
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            break;

                        case EntityState.Modified:
                            if (property.IsModified)
                            {
                                auditEntry.Action = "Update";
                                auditEntry.OldValues[propertyName] = property.OriginalValue;
                                auditEntry.NewValues[propertyName] = property.CurrentValue;
                            }
                            break;
                    }
                }
            }

            foreach (var auditEntry in auditEntries)
            {
                AuditLogs.Add(auditEntry.ToAuditLog());
            }
        }
    }
}