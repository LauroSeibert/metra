using Erp.Domain.Partners;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

internal sealed class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.ConfigureEntity();

        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(Partner.NameMaxLength).IsRequired();
        builder.Property(p => p.Document).HasMaxLength(Partner.DocumentMaxLength).IsRequired();
        builder.Property(p => p.DocumentKind).HasConversion<string>().HasMaxLength(4).IsRequired();
        builder.Property(p => p.Email).HasMaxLength(Partner.EmailMaxLength);
        builder.Property(p => p.Phone).HasMaxLength(Partner.PhoneMaxLength);
        builder.Property(p => p.Active).IsRequired();

        // Índices justificados em docs/modules/partners.md (EXPLAIN com 20 mil parceiros).
        builder.HasIndex(p => p.Document).IsUnique();
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => new { p.Type, p.Name });
        builder.HasIndex(p => new { p.Active, p.Name });
        builder.HasIndex(p => p.CreatedAt);
    }
}
