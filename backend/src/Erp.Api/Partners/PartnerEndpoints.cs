using Erp.Api.Access;
using Erp.Application.Common;
using Erp.Application.Partners;
using Erp.Domain.Partners;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Erp.Api.Partners;

/// <summary>Clientes e fornecedores. Mesmo padrão dos endpoints de produtos.</summary>
internal static class PartnerEndpoints
{
    public static RouteGroupBuilder MapPartnerEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/partners").WithTags("Partners");

        group.MapGet("/", ListAsync)
            .WithName("ListPartners")
            .WithSummary("Lista parceiros com busca por prefixo do nome ou do documento, filtros e ordenação.")
            .ProducesValidationProblem();

        group.MapGet("/{id:long}", GetAsync)
            .WithName("GetPartner")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .WithName("CreatePartner")
            .RequireWrite()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:long}", UpdateAsync)
            .WithName("UpdatePartner")
            .RequireWrite()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:long}/activate", ActivateAsync)
            .WithName("ActivatePartner")
            .RequireWrite()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:long}/deactivate", DeactivateAsync)
            .WithName("DeactivatePartner")
            .RequireWrite()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<Ok<PagedResult<PartnerListItemDto>>> ListAsync(
        PartnerService service,
        string? search,
        PartnerType? type,
        bool? active,
        string? sort,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service
            .ListAsync(new PartnerListQuery(search, type, active, sort, page, pageSize), cancellationToken)
            .ConfigureAwait(false));

    private static async Task<Ok<PartnerDto>> GetAsync(long id, PartnerService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAsync(id, cancellationToken).ConfigureAwait(false));

    private static async Task<Created<PartnerDto>> CreateAsync(
        CreatePartnerRequest request,
        PartnerService service,
        CancellationToken cancellationToken)
    {
        var partner = await service.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return TypedResults.Created($"/api/v1/partners/{partner.Id}", partner);
    }

    private static async Task<Ok<PartnerDto>> UpdateAsync(
        long id,
        UpdatePartnerRequest request,
        PartnerService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false));

    private static async Task<Ok<PartnerDto>> ActivateAsync(long id, PartnerService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.SetActiveAsync(id, true, cancellationToken).ConfigureAwait(false));

    private static async Task<Ok<PartnerDto>> DeactivateAsync(long id, PartnerService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.SetActiveAsync(id, false, cancellationToken).ConfigureAwait(false));
}
