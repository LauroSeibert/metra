using System.Security.Claims;
using Erp.Application.Abstractions;
using Erp.Domain.Access;
using Microsoft.AspNetCore.Authorization;

namespace Erp.Api.Access;

/// <summary>
/// Políticas de autorização. Toda rota exige usuário autenticado (política padrão); gravações
/// exigem <see cref="Write"/> (Admin ou Operator) e a administração exige <see cref="Admin"/>.
/// </summary>
internal static class Policies
{
    public const string Write = "write";
    public const string Admin = "admin";

    public static IServiceCollection AddErpAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Write, policy => policy.RequireRole(Roles.Admin, Roles.Operator))
            .AddPolicy(Admin, policy => policy.RequireRole(Roles.Admin));
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();
        return services;
    }

    /// <summary>Endpoint que grava dados operacionais: Viewer recebe 403.</summary>
    public static TBuilder RequireWrite<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.RequireAuthorization(Write);
}

/// <summary>Usuário da requisição, lido das claims do token.</summary>
internal sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public long Id => long.TryParse(Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id
        : throw new InvalidOperationException("Requisição sem usuário autenticado.");

    public string Role => Principal.FindFirstValue(ClaimTypes.Role) ?? Roles.Viewer;

    private ClaimsPrincipal Principal => accessor.HttpContext?.User
        ?? throw new InvalidOperationException("Fora de uma requisição HTTP.");
}
