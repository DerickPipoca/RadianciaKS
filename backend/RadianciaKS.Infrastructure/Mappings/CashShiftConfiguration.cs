using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Mappings
{
    public class CashShiftConfiguration : IEntityTypeConfiguration<CashShift>
    {
        public void Configure(EntityTypeBuilder<CashShift> builder)
        {
            builder.ToTable("CashShift");

            builder.HasKey(c => c.Id);
            builder.Property(c => c.CreatedAt);
            builder.Property(c => c.ClosedAt);
            builder.Property(c => c.Active);
            builder.HasIndex(c => c.TenantId);

            builder.Property(c => c.InitialBalance)
                .HasColumnType("numeric(10,2)");
            builder.Property(c => c.FinalReportedBalance)
                .HasColumnType("numeric(10,2)");
            builder.Property(c => c.FinalCalculatedBalance)
                .HasColumnType("numeric(10,2)");

            builder.Property(c => c.Status);

            builder.HasOne(c => c.EmployeeOpener)
               .WithMany()
               .HasForeignKey(c => c.EmployeeOpenerId)
               .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(c => c.EmployeeCloser)
                .WithMany()
                .HasForeignKey(c => c.EmployeeCloserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}