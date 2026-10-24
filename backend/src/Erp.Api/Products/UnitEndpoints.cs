using Erp.Api.Access;
using Erp.Application.Common;
using Erp.Application.Products;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Erp.Api.Products;

internal static class UnitEndpoints
{
    public static RouteGroupBuilder MapUnitEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/units").WithTags("Units");

        group.MapGet("/", ListAsync).WithName("ListUnits").ProducesValidationProblem();
        group.MapGet("/{id:long}", GetAsync).WithName("GetUnit").ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/", CreateAsync)
            .WithName("CreateUnit")
            .RequireWrite()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPut("/{id:long}", UpdateAsync)
            .WithName("UpdateUnit")
            .RequireWrite()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapDelete("/{id:long}", DeleteAsync)
            .WithName("DeleteUnit")
            .RequireWrite()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }

    private static async Task<Ok<PagedResult<UnitDto>>> ListAsync(
        UnitService service,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(search, page, pageSize, cancellationToken).ConfigureAwait(false));

    private static async Task<Ok<UnitDto>> GetAsync(long id, UnitService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAsync(id, cancellationToken).ConfigureAwait(false));

    private static async Task<Created<UnitDto>> CreateAsync(
        SaveUnitRequest request,
        UnitService service,
        CancellationToken cancellationToken)
    {
        var unit = await service.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return TypedResults.Created($"/api/v1/units/{unit.Id}", unit);
    }

    private static async Task<Ok<UnitDto>> UpdateAsync(
        long id,
        SaveUnitRequest request,
        UnitService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false));

    private static async Task<NoContent> DeleteAsync(long id, UnitService service, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return TypedResults.NoContent();
    }
}
