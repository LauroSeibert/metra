using Erp.Domain.Partners;
using Erp.Domain.Products;
using Erp.Domain.Settings;
using Erp.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Erp.Application.Abstractions;

/// <summary>Acesso a dados usado pelos casos de uso. Implementado por ErpDbContext na Infrastructure.</summary>
public interface IErpDbContext
{
    DbSet<Product> Products { get; }

    DbSet<ProductCategory> ProductCategories { get; }

    DbSet<Unit> Units { get; }

    DbSet<Partner> Partners { get; }

    DbSet<StockMovement> StockMovements { get; }

    DbSet<StockBalance> StockBalances { get; }

    DbSet<AppSetting> AppSettings { get; }

    /// <summary>Nomes dos usuários, para exibir quem registrou algo (o usuário em si fica no Identity).</summary>
    IQueryable<UserName> UserNames { get; }

    /// <summary>Para descartar alterações pendentes antes de refazer uma operação (conflito de versão).</summary>
    ChangeTracker ChangeTracker { get; }

    /// <summary>Transações explícitas, para casos de uso que gravam em mais de um passo.</summary>
    DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Projeção mínima do usuário para as telas: id e nome. Classe com `init` (não record posicional) para o
/// EF traduzir filtros sobre ela.
/// </summary>
public sealed class UserName
{
    public long Id { get; init; }

    public string FullName { get; init; } = string.Empty;
}
