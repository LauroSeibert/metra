using Erp.Application.Abstractions;
using Erp.Application.Access;
using Erp.Application.Common;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Erp.Api.Access;

/// <summary>Administração de usuários: somente Admin. Usuário nunca é excluído, só inativado.</summary>
internal static class UserEndpoints
{
    public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/users").WithTags("Users").RequireAuthorization(Policies.Admin);

        group.MapGet("/", ListAsync)
            .WithName("ListUsers")
            .ProducesValidationProblem();

        group.MapGet("/{id:long}", GetAsync)
            .WithName("GetUser")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .WithName("CreateUser")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:long}", UpdateAsync)
            .WithName("UpdateUser")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:long}/reset-password", ResetPasswordAsync)
            .WithName("ResetUserPassword")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<Ok<PagedResult<UserDto>>> ListAsync(
        IUserAccounts accounts,
        string? search,
        string? role,
        bool? active,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await accounts
            .ListAsync(new UserListQuery(search, role, active, page, pageSize), cancellationToken)
            .ConfigureAwait(false));

    private static async Task<Ok<UserDto>> GetAsync(long id, IUserAccounts accounts, CancellationToken cancellationToken) =>
        TypedResults.Ok(await accounts.GetAsync(id, cancellationToken).ConfigureAwait(false));

    private static async Task<Created<UserDto>> CreateAsync(
        CreateUserRequest request,
        IUserAccounts accounts,
        CancellationToken cancellationToken)
    {
        var user = await accounts.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return TypedResults.Created($"/api/v1/users/{user.Id}", user);
    }

    private static async Task<Ok<UserDto>> UpdateAsync(
        long id,
        UpdateUserRequest request,
        IUserAccounts accounts,
        ICurrentUser currentUser,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await accounts.UpdateAsync(id, currentUser.Id, request, cancellationToken).ConfigureAwait(false));

    private static async Task<NoContent> ResetPasswordAsync(
        long id,
        ResetPasswordRequest request,
        IUserAccounts accounts,
        CancellationToken cancellationToken)
    {
        await accounts.ResetPasswordAsync(id, request, cancellationToken).ConfigureAwait(false);
        return TypedResults.NoContent();
    }
}
