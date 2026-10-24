using System.Threading.RateLimiting;
using Erp.Application.Abstractions;
using Erp.Application.Access;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Erp.Api.Access;

/// <summary>Login, renovação e saída. Login e refresh são anônimos e têm limite de requisições por IP.</summary>
internal static class AuthEndpoints
{
    public const string LoginRateLimit = "login";

    public static IServiceCollection AddLoginRateLimit(this IServiceCollection services, IConfiguration configuration)
    {
        var permitLimit = configuration.GetValue("Auth:LoginRateLimit:PermitLimit", 20);
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(LoginRateLimit, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = permitLimit, Window = TimeSpan.FromMinutes(1) }));
        });
        return services;
    }

    public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/auth").WithTags("Auth");

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .WithSummary("Confere e-mail e senha e abre a sessão. 5 erros seguidos bloqueiam a conta por 15 min.")
            .AllowAnonymous()
            .RequireRateLimiting(LoginRateLimit)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapPost("/refresh", RefreshAsync)
            .WithName("RefreshSession")
            .AllowAnonymous()
            .RequireRateLimiting(LoginRateLimit)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/logout", LogoutAsync)
            .WithName("Logout")
            .WithSummary("Encerra a sessão: os refresh tokens já emitidos param de valer.");

        return api;
    }

    private static async Task<Ok<SessionTokensDto>> LoginAsync(
        LoginRequest request,
        IUserAccounts accounts,
        ISessionTokens tokens,
        CancellationToken cancellationToken)
    {
        var userId = await accounts.VerifyCredentialsAsync(request, cancellationToken).ConfigureAwait(false);
        return TypedResults.Ok(await tokens.IssueAsync(userId, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<Ok<SessionTokensDto>> RefreshAsync(
        RefreshRequest request,
        ISessionTokens tokens,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await tokens.RefreshAsync(request.RefreshToken, cancellationToken).ConfigureAwait(false));

    private static async Task<NoContent> LogoutAsync(
        IUserAccounts accounts,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await accounts.RevokeSessionsAsync(currentUser.Id, cancellationToken).ConfigureAwait(false);
        return TypedResults.NoContent();
    }
}
