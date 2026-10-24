using Erp.Application.Abstractions;
using Erp.Application.Access;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Erp.Api.Access;

/// <summary>
/// "Meu perfil". As rotas não recebem id: cada usuário lê e edita só o próprio perfil, identificado
/// pelo token. Vale para todos os perfis, inclusive Viewer.
/// </summary>
internal static class ProfileEndpoints
{
    public static RouteGroupBuilder MapProfileEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/me").WithTags("Profile");

        group.MapGet("/", GetAsync).WithName("GetMyProfile");

        group.MapPut("/", UpdateAsync)
            .WithName("UpdateMyProfile")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/password", ChangePasswordAsync)
            .WithName("ChangeMyPassword")
            .ProducesValidationProblem();

        return api;
    }

    private static async Task<Ok<UserProfileDto>> GetAsync(
        IUserAccounts accounts,
        ICurrentUser currentUser,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await accounts.GetProfileAsync(currentUser.Id, cancellationToken).ConfigureAwait(false));

    private static async Task<Ok<UserProfileDto>> UpdateAsync(
        UpdateProfileRequest request,
        IUserAccounts accounts,
        ICurrentUser currentUser,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await accounts.UpdateProfileAsync(currentUser.Id, request, cancellationToken).ConfigureAwait(false));

    private static async Task<NoContent> ChangePasswordAsync(
        ChangePasswordRequest request,
        IUserAccounts accounts,
        ICurrentUser currentUser,
        CancellationToken cancellationToken)
    {
        await accounts.ChangePasswordAsync(currentUser.Id, request, cancellationToken).ConfigureAwait(false);
        return TypedResults.NoContent();
    }
}
