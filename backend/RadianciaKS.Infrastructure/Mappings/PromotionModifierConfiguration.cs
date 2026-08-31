using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Mappings
{
    public class PromotionModifierConfiguration : IEntityTypeConfiguration<PromotionModifier>
    {
        public void Configure(EntityTypeBuilder<PromotionModifier> builder)
        {
            builder.ToTable("PromotionModifiers");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Active);
            builder.Property(p => p.CreatedAt);
            builder.HasIndex(p => p.TenantId);

            builder.Property(p => p.OverridePrice)
                .IsRequired()
                    .HasColumnType("numeric(10,2)");

            builder.HasOne(pm => pm.Promotion)
                .WithMany(p => p.PromotionModifiers)
                .HasForeignKey(pm => pm.PromotionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pm => pm.ModifierOption)
                .WithMany()
                .HasForeignKey(pm => pm.ModifierOptionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}