using Erp.Domain.Common;
using Erp.Domain.Products;
using Erp.Domain.Settings;
using Erp.Domain.Stock;
using Erp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ConfigureEntity();

        builder.Property(m => m.Type).HasConversion<string>().HasMaxLength(12).IsRequired();
        builder.Property(m => m.Quantity).HasPrecision(DecimalLimits.QuantityPrecision, DecimalLimits.QuantityScale).IsRequired();
        builder.Property(m => m.UnitCost).HasPrecision(DecimalLimits.MoneyPrecision, DecimalLimits.MoneyScale);
        builder.Property(m => m.OccurredAt).IsRequired();
        builder.Property(m => m.ReferenceType).HasMaxLength(StockMovement.ReferenceTypeMaxLength);
        builder.Property(m => m.Note).HasMaxLength(StockMovement.NoteMaxLength);

        builder.HasOne<Product>().WithMany().HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppUser>().WithMany().HasForeignKey(m => m.UserId).OnDelete(DeleteBehavior.Restrict);

        // Índices justificados em docs/modules/stock.md (EXPLAIN com 1 milhão de movimentações).
        builder.HasIndex(m => new { m.ProductId, m.OccurredAt, m.Id });
        builder.HasIndex(m => new { m.OccurredAt, m.Id });
        builder.HasIndex(m => new { m.ReferenceType, m.ReferenceId });
    }
}

internal sealed class StockBalanceConfiguration : IEntityTypeConfiguration<StockBalance>
{
    public void Configure(EntityTypeBuilder<StockBalance> builder)
    {
        builder.ConfigureEntity();

        builder.Property(b => b.Quantity).HasPrecision(DecimalLimits.QuantityPrecision, DecimalLimits.QuantityScale).IsRequired();
        builder.HasOne<Product>().WithMany().HasForeignKey(b => b.ProductId).OnDelete(DeleteBehavior.Restrict);

        // Uma linha por produto.
        builder.HasIndex(b => b.ProductId).IsUnique();
    }
}

internal sealed class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ConfigureEntity();

        builder.Property(s => s.Key).HasMaxLength(AppSetting.KeyMaxLength).IsRequired();
        builder.Property(s => s.Value).HasMaxLength(AppSetting.ValueMaxLength).IsRequired();
        builder.HasIndex(s => s.Key).IsUnique();
    }
}
