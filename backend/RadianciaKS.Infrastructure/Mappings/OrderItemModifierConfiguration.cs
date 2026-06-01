using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Mappings
{
    public class OrderItemModifierConfiguration : IEntityTypeConfiguration<OrderItemModifier>
    {
        public void Configure(EntityTypeBuilder<OrderItemModifier> builder)
        {
            builder.ToTable("OrderItemModifiers");

            builder.HasKey(m => m.Id);
            builder.Property(p => p.CreatedAt);
            builder.Property(p => p.Active);

            builder.Property(m => m.Name).IsRequired().HasMaxLength(100);
            builder.Property(m => m.AdditionalPrice).HasColumnType("numeric(10,2)");

            builder.HasOne(m => m.OrderItem)
                   .WithMany(i => i.SelectedModifiers)
                   .HasForeignKey(m => m.OrderItemId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}