using Microsoft.EntityFrameworkCore;
using RadianciaKS.Domain.Models;

namespace RadianciaKS.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<Category> Categories { get; }
        DbSet<Product> Products { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}