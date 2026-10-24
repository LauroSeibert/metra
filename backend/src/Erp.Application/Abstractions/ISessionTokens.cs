using Erp.Application.Access;

namespace Erp.Application.Abstractions;

/// <summary>Emite e renova os tokens da sessão. Implementado na Infrastructure.</summary>
public interface ISessionTokens
{
    Task<SessionTokensDto> IssueAsync(long userId, CancellationToken cancellationToken);

    /// <summary>Troca um refresh token válido por tokens novos; inválido, expirado ou revogado → 401.</summary>
    Task<SessionTokensDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
}
