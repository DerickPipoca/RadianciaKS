using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Infrastructure.Mappings
{
    public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
    {
        public void Configure(EntityTypeBuilder<Employee> builder)
        {
            builder.ToTable("Employees");

            builder.HasKey(c => c.Id);
            builder.Property(p => p.CreatedAt);
            builder.Property(p => p.Active);
            builder.HasIndex(o => o.TenantId);

            builder.Property(c => c.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(p => p.Birthday);

            builder.Property(p => p.CPF)
                .IsRequired()
                .HasMaxLength(11)
                .IsFixedLength();

            builder.HasIndex(e => new { e.TenantId, e.CPF }).IsUnique();

            builder.Property(p => p.Role);

            builder.Property(c => c.PasswordHash)
                .HasMaxLength(3000);
        }
    }
}