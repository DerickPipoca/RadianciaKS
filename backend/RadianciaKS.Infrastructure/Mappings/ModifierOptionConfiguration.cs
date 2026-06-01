using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Mappings
{
    public class ModifierOptionConfiguration : IEntityTypeConfiguration<ModifierOption>
    {
        public void Configure(EntityTypeBuilder<ModifierOption> builder)
        {
            builder.ToTable("ModifierOptions");

            builder.HasKey(o => o.Id);
            builder.Property(p => p.CreatedAt);
            builder.Property(p => p.Active);
            builder.HasIndex(o => o.TenantId);

            builder.Property(o => o.Name).IsRequired().HasMaxLength(100);

            builder.Property(o => o.AdditionalPrice).HasColumnType("numeric(10,2)");

        }
    }
}