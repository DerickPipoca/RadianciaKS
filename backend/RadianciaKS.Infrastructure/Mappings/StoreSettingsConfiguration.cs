using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Mappings
{
    public class StoreSettingsConfiguration : IEntityTypeConfiguration<StoreSettings>
    {
        public void Configure(EntityTypeBuilder<StoreSettings> builder)
        {
            builder.ToTable("StoreSettings");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Active);
            builder.Property(p => p.CreatedAt);
            builder.HasIndex(p => p.TenantId);

            builder.Property(p => p.StoreName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.CNPJ)
                .IsRequired()
                .HasMaxLength(14);

            builder.Property(p => p.Address)
                .HasMaxLength(255);

            builder.Property(p => p.Phone)
                .HasMaxLength(20);

            builder.Property(p => p.SmallLogoPath)
                .HasMaxLength(500);

            builder.Property(p => p.BigLogoPath)
                .HasMaxLength(500);

            builder.Property(p => p.ReceiptFooter)
                .HasMaxLength(256);

            builder.Property(p => p.ServiceCharge)
                .IsRequired()
                .HasColumnType("numeric(10,2)");
        }
    }
}