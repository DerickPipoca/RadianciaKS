using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Mappings
{
    public class SystemLicenseConfiguration : IEntityTypeConfiguration<SystemLicense>
    {
        public void Configure(EntityTypeBuilder<SystemLicense> builder)
        {
            builder.ToTable("SystemLicenses");

            builder.HasKey(l => l.Id);

            builder.Property(l => l.LicenseKey)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(l => l.LicenseKey)
                .IsUnique();

            builder.Property(l => l.Status)
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(l => l.ExpiresAt)
                .IsRequired();

            builder.Property(l => l.LastValidatedAt);

            builder.Property(l => l.LastKnownSystemTime)
                .IsRequired();

            builder.Property(l => l.LicenseFile)
                .HasColumnType("text");

            builder.Property(l => l.CreatedAt)
                .IsRequired();

            builder.Property(l => l.UpdatedAt);
        }
    }
}