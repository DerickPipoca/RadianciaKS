using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Mappings
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Active);
            builder.Property(p => p.CreatedAt);
            builder.HasIndex(p => p.TenantId);

            builder.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.ImagePath)
                .HasMaxLength(256);

            builder.Property(p => p.Price)
                .HasColumnType("numeric(10,2)");

            builder.HasOne(p => p.Category).WithMany().HasForeignKey(p => p.CategoryId);
        }
    }
}