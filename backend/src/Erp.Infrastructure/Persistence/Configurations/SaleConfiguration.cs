using Erp.Domain.Common;
using Erp.Domain.Partners;
using Erp.Domain.Products;
using Erp.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ConfigureEntity();

        builder.Property(s => s.Number).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(s => s.PaymentMethod).HasConversion<string>().HasMaxLength(12).IsRequired();
        builder.Property(s => s.Discount).HasPrecision(DecimalLimits.MoneyPrecision, DecimalLimits.MoneyScale).IsRequired();
        builder.Property(s => s.Subtotal).HasPrecision(DecimalLimits.MoneyPrecision, DecimalLimits.MoneyScale).IsRequired();
        builder.Property(s => s.Total).HasPrecision(DecimalLimits.MoneyPrecision, DecimalLimits.MoneyScale).IsRequired();
        builder.Property(s => s.CancellationReason).HasMaxLength(Sale.ReasonMaxLength);
        builder.Property(s => s.Note).HasMaxLength(Sale.NoteMaxLength);
        builder.Ignore(s => s.Gross);

        builder.HasOne<Partner>().WithMany().HasForeignKey(s => s.PartnerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(s => s.Items).WithOne().HasForeignKey(i => i.SaleId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(s => s.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Índices justificados em docs/modules/sales.md (EXPLAIN com 200 mil vendas).
        builder.HasIndex(s => s.Number).IsUnique();
        builder.HasIndex(s => new { s.Status, s.IssuedAt });
        builder.HasIndex(s => new { s.PartnerId, s.IssuedAt });
        builder.HasIndex(s => s.IssuedAt);
    }
}

internal sealed class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ConfigureEntity();

        builder.Property(i => i.Quantity).HasPrecision(DecimalLimits.QuantityPrecision, DecimalLimits.QuantityScale).IsRequired();
        builder.Property(i => i.UnitPrice).HasPrecision(DecimalLimits.MoneyPrecision, DecimalLimits.MoneyScale).IsRequired();
        builder.Property(i => i.Discount).HasPrecision(DecimalLimits.MoneyPrecision, DecimalLimits.MoneyScale).IsRequired();
        builder.Property(i => i.Total).HasPrecision(DecimalLimits.MoneyPrecision, DecimalLimits.MoneyScale).IsRequired();

        builder.HasOne<Product>().WithMany().HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DocumentSequenceConfiguration : IEntityTypeConfiguration<DocumentSequence>
{
    public void Configure(EntityTypeBuilder<DocumentSequence> builder)
    {
        builder.ConfigureEntity();

        builder.Property(s => s.Name).HasMaxLength(DocumentSequence.NameMaxLength).IsRequired();
        builder.HasIndex(s => s.Name).IsUnique();
    }
}
