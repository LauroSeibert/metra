using Erp.Application.Access;
using Erp.Application.Common;

namespace Erp.Application.Abstractions;

/// <summary>
/// Contas de usuário. Implementado na Infrastructure com o ASP.NET Core Identity, que é quem trata
/// senha, bloqueio e perfis; a Application só conhece este contrato.
/// </summary>
public interface IUserAccounts
{
    /// <summary>Confere e-mail e senha (contando tentativas para o bloqueio) e devolve o id do usuário.</summary>
    Task<long> VerifyCredentialsAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<UserProfileDto> GetProfileAsync(long userId, CancellationToken cancellationToken);

    Task<UserProfileDto> UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken cancellationToken);

    Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken cancellationToken);

    /// <summary>Invalida os tokens de refresh já emitidos (sair).</summary>
    Task RevokeSessionsAsync(long userId, CancellationToken cancellationToken);

    Task<PagedResult<UserDto>> ListAsync(UserListQuery query, CancellationToken cancellationToken);

    Task<UserDto> GetAsync(long id, CancellationToken cancellationToken);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken);

    Task<UserDto> UpdateAsync(long id, long actingUserId, UpdateUserRequest request, CancellationToken cancellationToken);

    Task ResetPasswordAsync(long id, ResetPasswordRequest request, CancellationToken cancellationToken);
}
