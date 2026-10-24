using Erp.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

/// <summary>Mapeamento comum: chave BIGINT auto incremento, datas de auditoria e versão.</summary>
internal static class EntityConfiguration
{
    public static void ConfigureEntity<T>(this EntityTypeBuilder<T> builder)
        where T : Entity
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.UpdatedAt).IsRequired();
        builder.Property(e => e.Version).IsRequired().IsConcurrencyToken();
    }
}
