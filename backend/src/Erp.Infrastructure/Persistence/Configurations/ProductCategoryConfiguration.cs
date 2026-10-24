using Erp.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ConfigureEntity();
        builder.Property(c => c.Name).HasMaxLength(ProductCategory.NameMaxLength).IsRequired();
        builder.HasIndex(c => c.Name).IsUnique();
    }
}
