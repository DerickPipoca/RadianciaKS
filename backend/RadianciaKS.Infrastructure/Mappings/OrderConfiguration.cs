using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Mappings
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.ToTable("Orders");

            builder.HasKey(o => o.Id);
            builder.Property(p => p.CreatedAt);
            builder.Property(p => p.Active);
            builder.HasIndex(o => o.TenantId);

            builder.Property(o => o.TableNumber)
                .HasMaxLength(32);

            builder.Property(o => o.ReceiptUrl);

            builder.Property(o => o.OrderStatus);

            builder.Property(o => o.TotalAmount)
                .HasColumnType("numeric(10,2)");
        }
    }
}