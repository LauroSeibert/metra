namespace Erp.Application.Access;

/// <summary>Tema da interface escolhido pelo usuário.</summary>
public enum ThemePreference
{
    Light = 1,
    Dark = 2,
}

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

/// <summary>Tokens da sessão: o de acesso vai no cabeçalho Authorization; o de refresh renova a sessão.</summary>
public sealed record SessionTokensDto(string AccessToken, long ExpiresIn, string RefreshToken);

/// <summary>Perfil do usuário logado (tela "Meu perfil" e cabeçalho do app).</summary>
public sealed record UserProfileDto(
    long Id,
    string Email,
    string FullName,
    string Role,
    ThemePreference Theme,
    int PageSize,
    int Version);

public sealed record UpdateProfileRequest(string FullName, ThemePreference Theme, int PageSize, int Version);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed record UserListQuery(string? Search, string? Role, bool? Active, int? Page, int? PageSize);

/// <summary>Usuário na administração (somente Admin).</summary>
public sealed record UserDto(
    long Id,
    string Email,
    string FullName,
    string Role,
    bool Active,
    bool LockedOut,
    int Version,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record CreateUserRequest(string Email, string FullName, string Role, string Password);

public sealed record UpdateUserRequest(string FullName, string Role, bool Active, int Version);

public sealed record ResetPasswordRequest(string NewPassword);
