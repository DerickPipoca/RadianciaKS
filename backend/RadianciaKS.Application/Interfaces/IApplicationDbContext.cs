using Microsoft.EntityFrameworkCore;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }
        DbSet<Category> Categories { get; }
        DbSet<Product> Products { get; }
        DbSet<Order> Orders { get; }
        DbSet<CashShift> CashShifts { get; set; }
        DbSet<OrderItem> OrderItems { get; }
        DbSet<Payment> Payments { get; }
        DbSet<Employee> Employees { get; }
        DbSet<StoreSettings> StoreSettings { get; }
        DbSet<ModifierGroup> ModifierGroups { get; }
        DbSet<ModifierOption> ModifierOptions { get; }
        DbSet<OrderItemModifier> OrderItemModifiers { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}