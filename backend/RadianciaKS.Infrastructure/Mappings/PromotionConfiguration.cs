using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Mappings
{
    public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
    {
        public void Configure(EntityTypeBuilder<Promotion> builder)
        {
            builder.ToTable("Promotions");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Active);
            builder.Property(p => p.CreatedAt);
            builder.HasIndex(p => p.TenantId);

            builder.Property(p => p.Name)
                .IsRequired()
                    .HasMaxLength(100);
            builder.Property(p => p.Description)
                .IsRequired()
                    .HasMaxLength(256);
            builder.Property(p => p.PromotionalPrice)
                .HasColumnType("numeric(10,2)");

            builder.HasOne(p => p.BaseProduct)
                .WithMany()
                .HasForeignKey(p => p.BaseProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}