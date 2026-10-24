using System.Globalization;
using Erp.Application.Abstractions;
using Erp.Application.Access;
using Erp.Application.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Erp.Infrastructure.Identity;

/// <summary>
/// Tokens da sessão no formato do esquema bearer do ASP.NET Core (protegidos pelo Data Protection,
/// sem dependência de JWT). O refresh confere o security stamp do usuário, então trocar a senha,
/// sair, mudar o perfil ou inativar a conta invalida os refresh tokens já emitidos.
/// </summary>
internal sealed class SessionTokens(
    IOptionsMonitor<BearerTokenOptions> bearerOptions,
    SignInManager<AppUser> signIn,
    UserManager<AppUser> users,
    TimeProvider clock) : ISessionTokens
{
    public async Task<SessionTokensDto> IssueAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture)).ConfigureAwait(false)
            ?? throw new UnauthorizedException();
        return await IssueAsync(user).ConfigureAwait(false);
    }

    public async Task<SessionTokensDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var options = Options;
        var ticket = string.IsNullOrWhiteSpace(refreshToken) ? null : options.RefreshTokenProtector.Unprotect(refreshToken);
        if (ticket?.Properties.ExpiresUtc is not { } expires || clock.GetUtcNow() >= expires)
        {
            throw InvalidRefresh();
        }

        var user = await signIn.ValidateSecurityStampAsync(ticket.Principal).ConfigureAwait(false);
        if (user is null || !user.Active)
        {
            throw InvalidRefresh();
        }

        return await IssueAsync(user).ConfigureAwait(false);
    }

    private BearerTokenOptions Options => bearerOptions.Get(IdentityConstants.BearerScheme);

    private async Task<SessionTokensDto> IssueAsync(AppUser user)
    {
        var options = Options;
        var principal = await signIn.CreateUserPrincipalAsync(user).ConfigureAwait(false);
        var now = clock.GetUtcNow();

        var access = options.BearerTokenProtector.Protect(Ticket(principal, now + options.BearerTokenExpiration));
        var refresh = options.RefreshTokenProtector.Protect(Ticket(principal, now + options.RefreshTokenExpiration));
        return new SessionTokensDto(access, (long)options.BearerTokenExpiration.TotalSeconds, refresh);
    }

    private static AuthenticationTicket Ticket(System.Security.Claims.ClaimsPrincipal principal, DateTimeOffset expires) =>
        new(principal, new AuthenticationProperties { ExpiresUtc = expires }, IdentityConstants.BearerScheme);

    private static UnauthorizedException InvalidRefresh() =>
        new(ErrorCodes.InvalidRefreshToken, "Sua sessão expirou. Entre de novo.");
}
