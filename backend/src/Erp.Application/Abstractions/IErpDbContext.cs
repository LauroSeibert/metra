using Erp.Domain.Partners;
using Erp.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Erp.Application.Abstractions;

/// <summary>Acesso a dados usado pelos casos de uso. Implementado por ErpDbContext na Infrastructure.</summary>
public interface IErpDbContext
{
    DbSet<Product> Products { get; }

    DbSet<ProductCategory> ProductCategories { get; }

    DbSet<Unit> Units { get; }

    DbSet<Partner> Partners { get; }

    /// <summary>Transações explícitas, para casos de uso que gravam em mais de um passo.</summary>
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
