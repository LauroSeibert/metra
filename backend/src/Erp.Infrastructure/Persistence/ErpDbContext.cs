using Erp.Application.Abstractions;
using Erp.Domain.Common;
using Erp.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Persistence;

public sealed class ErpDbContext(DbContextOptions<ErpDbContext> options, TimeProvider timeProvider)
    : DbContext(options), IErpDbContext
{
    public DbSet<Product> Products => Set<Product>();

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();

    public DbSet<Unit> Units => Set<Unit>();

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

        // Texto em utf8mb4 é o padrão do servidor; strings sem tamanho explícito viram VARCHAR(255).
        configurationBuilder.Properties<string>().HaveMaxLength(255);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ErpDbContext).Assembly);
        modelBuilder.ApplySnakeCaseNames();
    }

    /// <summary>Preenche datas de criação e alteração e incrementa a versão (concorrência otimista).</summary>
    private void StampEntities()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (var entry in ChangeTracker.Entries<Entity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(e => e.CreatedAt).CurrentValue = now;
                    entry.Property(e => e.UpdatedAt).CurrentValue = now;
                    entry.Property(e => e.Version).CurrentValue = 1;
                    break;
                case EntityState.Modified:
                    entry.Property(e => e.UpdatedAt).CurrentValue = now;
                    entry.Property(e => e.Version).CurrentValue = entry.Property(e => e.Version).OriginalValue + 1;
                    break;
                default:
                    break;
            }
        }
    }
}
