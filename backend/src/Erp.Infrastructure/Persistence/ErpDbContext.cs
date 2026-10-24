using Erp.Application.Abstractions;
using Erp.Domain.Common;
using Erp.Domain.Finance;
using Erp.Domain.Partners;
using Erp.Domain.Products;
using Erp.Domain.Sales;
using Erp.Domain.Settings;
using Erp.Domain.Stock;
using Erp.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Persistence;

public sealed class ErpDbContext(DbContextOptions<ErpDbContext> options, TimeProvider timeProvider)
    : IdentityDbContext<AppUser, AppRole, long>(options), IErpDbContext
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    public DbSet<Unit> Units => Set<Unit>();

    public DbSet<Partner> Partners => Set<Partner>();

    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    public DbSet<StockBalance> StockBalances => Set<StockBalance>();

    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    public DbSet<FinancialCategory> FinancialCategories => Set<FinancialCategory>();

    public DbSet<FinancialEntry> FinancialEntries => Set<FinancialEntry>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<DocumentSequence> DocumentSequences => Set<DocumentSequence>();

    public IQueryable<UserName> UserNames => Users.Select(u => new UserName { Id = u.Id, FullName = u.FullName });

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        StampEntities();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        // Datas sempre em UTC, DATETIME(6). O MySQL não guarda o Kind, então a leitura marca UTC.
        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("datetime(6)")
            .HaveConversion<UtcDateTimeConverter>();

        // Datas de calendário (DateOnly) em DATE; o provider precisa do conversor para ler.
        configurationBuilder.Properties<DateOnly>()
            .HaveColumnType("date")
            .HaveConversion<DateOnlyConverter>();

        // Texto em utf8mb4 é o padrão do servidor; strings sem tamanho explícito viram VARCHAR(255).
        configurationBuilder.Properties<string>().HaveMaxLength(255);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        // Esquema do Identity primeiro; a convenção de nomes vale para as tabelas dele também.
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ErpDbContext).Assembly);
        builder.ApplySnakeCaseNames();
    }

    /// <summary>Preenche datas de criação e alteração e incrementa a versão (concorrência otimista).</summary>
    private void StampEntities()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(IAuditable.CreatedAt)).CurrentValue = now;
                    entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;
                    entry.Property(nameof(IAuditable.Version)).CurrentValue = 1;
                    break;
                case EntityState.Modified:
                    entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;
                    var version = entry.Property(nameof(IAuditable.Version));
                    version.CurrentValue = (int)version.OriginalValue! + 1;
                    break;
                default:
                    break;
            }
        }
    }
}
