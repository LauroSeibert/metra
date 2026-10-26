using Erp.Domain.Common;
using Erp.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ConfigureEntity();

        builder.Property(p => p.Code).IsRequired();
        builder.Property(p => p.Sku).HasMaxLength(Product.SkuMaxLength).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(Product.NameMaxLength).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(Product.DescriptionMaxLength);
        builder.Property(p => p.CostPrice).HasPrecision(DecimalLimits.MoneyPrecision, DecimalLimits.MoneyScale);
        builder.Property(p => p.SalePrice).HasPrecision(DecimalLimits.MoneyPrecision, DecimalLimits.MoneyScale);
        builder.Property(p => p.MinStock).HasPrecision(DecimalLimits.QuantityPrecision, DecimalLimits.QuantityScale);
        builder.Property(p => p.Active).IsRequired();

        builder.HasOne(p => p.Category)
            .WithMany()
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Unit)
            .WithMany()
            .HasForeignKey(p => p.UnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índices justificados em docs/modules/products.md (EXPLAIN com 100 mil produtos).
        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.Sku).IsUnique();
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => new { p.CategoryId, p.Name });
        builder.HasIndex(p => new { p.Active, p.Name });
        builder.HasIndex(p => p.SalePrice);
        builder.HasIndex(p => p.CreatedAt);
    }
}
