using Erp.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class UnitConfiguration : IEntityTypeConfiguration<Unit>
{
    public void Configure(EntityTypeBuilder<Unit> builder)
    {
        builder.ConfigureEntity();
        builder.Property(u => u.Code).HasMaxLength(Unit.CodeMaxLength).IsRequired();
        builder.Property(u => u.Name).HasMaxLength(Unit.NameMaxLength).IsRequired();
        builder.HasIndex(u => u.Code).IsUnique();
    }
}
