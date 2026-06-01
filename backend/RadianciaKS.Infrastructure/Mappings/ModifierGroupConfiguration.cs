using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Mappings
{
    public class ModifierGroupConfiguration : IEntityTypeConfiguration<ModifierGroup>
    {
        public void Configure(EntityTypeBuilder<ModifierGroup> builder)
        {
            builder.ToTable("ModifierGroups");

            builder.HasKey(g => g.Id);
            builder.Property(p => p.CreatedAt);
            builder.Property(p => p.Active);
            builder.HasIndex(o => o.TenantId);

            builder.Property(g => g.Name).IsRequired().HasMaxLength(100);

            builder.HasMany(g => g.Options)
                   .WithOne(o => o.ModifierGroup)
                   .HasForeignKey(o => o.ModifierGroupId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}