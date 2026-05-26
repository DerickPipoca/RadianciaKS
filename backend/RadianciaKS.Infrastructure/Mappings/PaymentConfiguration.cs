using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Mappings
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payment");

            builder.HasKey(p => p.Id);

            builder.Property(p => p.Amount)
                .HasColumnType("numeric(10,2)");

            builder.Property(p => p.Method);

            builder.HasOne(p => p.Order)
                .WithMany(o => o.Payment)
                .HasForeignKey(p => p.OrderId);

            builder.HasIndex(o => o.TenantId);
        }
    }
}