using Microsoft.EntityFrameworkCore;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Category> Categories { get; }
        DbSet<Product> Products { get; }
        DbSet<Order> Orders { get; }
        DbSet<OrderItem> OrderItems { get; }
        DbSet<Payment> Payments { get; }
        DbSet<Employee> Employees { get; }
        DbSet<ModifierGroup> ModifierGroups { get; }
        DbSet<ModifierOption> ModifierOptions { get; }
        DbSet<OrderItemModifier> OrderItemModifiers { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}