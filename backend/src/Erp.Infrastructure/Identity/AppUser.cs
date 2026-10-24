using Erp.Application.Access;
using Erp.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Erp.Infrastructure.Identity;

/// <summary>
/// Usuário do sistema (tabela <c>app_user</c>). Senha, bloqueio e perfis ficam com o ASP.NET Core
/// Identity; o e-mail é também o nome de login.
/// </summary>
public sealed class AppUser : IdentityUser<long>, IAuditable
{
    public const int FullNameMaxLength = 100;
    public const int DefaultPageSize = 20;

    public string FullName { get; set; } = string.Empty;

    /// <summary>Usuário inativo não entra nem renova a sessão. Usuário nunca é excluído.</summary>
    public bool Active { get; set; } = true;

    public ThemePreference Theme { get; set; } = ThemePreference.Light;

    public int PageSize { get; set; } = DefaultPageSize;

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public int Version { get; private set; }
}

/// <summary>Perfil de acesso (tabela <c>app_role</c>): Admin, Operator ou Viewer.</summary>
public sealed class AppRole : IdentityRole<long>
{
    public AppRole()
    {
    }

    public AppRole(string name)
        : base(name)
    {
    }
}
