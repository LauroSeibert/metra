using Erp.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Erp.Infrastructure.Persistence.Configurations;

/// <summary>Colunas próprias do usuário; o resto do esquema do Identity vem de IdentityDbContext.</summary>
internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(u => u.FullName).HasMaxLength(AppUser.FullNameMaxLength).IsRequired();
        builder.Property(u => u.Active).IsRequired();
        builder.Property(u => u.Theme).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(u => u.PageSize).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();
        builder.Property(u => u.UpdatedAt).IsRequired();
        builder.Property(u => u.Version).IsRequired().IsConcurrencyToken();

        // Listagem de usuários ordenada por nome (tela de administração).
        builder.HasIndex(u => u.FullName);
    }
}
