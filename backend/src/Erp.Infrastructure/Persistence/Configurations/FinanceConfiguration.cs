using Erp.Domain.Common;
using Erp.Domain.Finance;
using Erp.Domain.Partners;
using Erp.Domain.Sales;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class FinancialCategoryConfiguration : IEntityTypeConfiguration<FinancialCategory>
{
    public void Configure(EntityTypeBuilder<FinancialCategory> builder)
    {
        builder.ConfigureEntity();

        builder.Property(c => c.Name).HasMaxLength(FinancialCategory.NameMaxLength).IsRequired();
        builder.Property(c => c.Kind).HasConversion<string>().HasMaxLength(10).IsRequired();

        // Nome único dentro do tipo (pode haver "Outros" de receita e de despesa).
        builder.HasIndex(c => new { c.Kind, c.Name }).IsUnique();
    }
}

internal sealed class FinancialEntryConfiguration : IEntityTypeConfiguration<FinancialEntry>
{
    public void Configure(EntityTypeBuilder<FinancialEntry> builder)
    {
        builder.ConfigureEntity();

        builder.Property(e => e.Kind).HasConversion<string>().HasMaxLength(12).IsRequired();
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(FinancialEntry.DescriptionMaxLength).IsRequired();
        builder.Property(e => e.Amount).HasPrecision(DecimalLimits.MoneyPrecision, DecimalLimits.MoneyScale).IsRequired();
        builder.Property(e => e.PaidAmount).HasPrecision(DecimalLimits.MoneyPrecision, DecimalLimits.MoneyScale);
        builder.Property(e => e.DueDate).HasColumnType("date").IsRequired();
        builder.Property(e => e.PaidAt).HasColumnType("date");
        builder.Property(e => e.ReversalReason).HasMaxLength(FinancialEntry.ReasonMaxLength);

        builder.HasOne<FinancialCategory>().WithMany().HasForeignKey(e => e.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Partner>().WithMany().HasForeignKey(e => e.PartnerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sale>().WithMany().HasForeignKey(e => e.SaleId).OnDelete(DeleteBehavior.Restrict);

        // Índices justificados em docs/modules/finance.md (EXPLAIN com o seed completo).
        builder.HasIndex(e => new { e.Kind, e.Status, e.DueDate, e.Id });
        builder.HasIndex(e => new { e.Kind, e.DueDate, e.Id });
        builder.HasIndex(e => new { e.Kind, e.PaidAt });
        builder.HasIndex(e => e.SaleId);
    }
}
