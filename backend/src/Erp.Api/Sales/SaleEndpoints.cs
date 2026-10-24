using Erp.Api.Access;
using Erp.Application.Common;
using Erp.Application.Sales;
using Erp.Domain.Sales;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Erp.Api.Sales;

/// <summary>Vendas: rascunho, confirmação (estoque + parcelas) e cancelamento.</summary>
internal static class SaleEndpoints
{
    public static RouteGroupBuilder MapSaleEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/sales").WithTags("Sales");

        group.MapGet("/", ListAsync)
            .WithName("ListSales")
            .WithSummary("Vendas por período, situação, cliente ou número.")
            .ProducesValidationProblem();

        group.MapGet("/settings", SettingsAsync)
            .WithName("GetSaleSettings")
            .WithSummary("Desconto máximo e número máximo de parcelas.");

        group.MapGet("/{id:long}", GetAsync)
            .WithName("GetSale")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .WithName("CreateSale")
            .WithSummary("Cria o rascunho com o próximo número; o preço de cada item vem do produto.")
            .RequireWrite()
            .ProducesValidationProblem();

        group.MapPut("/{id:long}", UpdateAsync)
            .WithName("UpdateSale")
            .WithSummary("Edita o rascunho (itens são substituídos).")
            .RequireWrite()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:long}/confirm", ConfirmAsync)
            .WithName("ConfirmSale")
            .WithSummary("Confirma: baixa o estoque e gera as parcelas a receber, numa transação.")
            .RequireWrite()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:long}/cancel", CancelAsync)
            .WithName("CancelSale")
            .WithSummary("Cancela; na confirmada, devolve o estoque e estorna e cancela as parcelas.")
            .RequireWrite()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }

    private static async Task<Ok<PagedResult<SaleListItemDto>>> ListAsync(
        SaleService service,
        int? number,
        long? partnerId,
        SaleStatus? status,
        DateOnly? issuedFrom,
        DateOnly? issuedTo,
        string? sort,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service
            .ListAsync(new SaleListQuery(number, partnerId, status, issuedFrom, issuedTo, sort, page, pageSize), cancellationToken)
            .ConfigureAwait(false));

    private static async Task<Ok<SaleSettingsDto>> SettingsAsync(SaleService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetSettingsAsync(cancellationToken).ConfigureAwait(false));

    private static async Task<Ok<SaleDto>> GetAsync(long id, SaleService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAsync(id, cancellationToken).ConfigureAwait(false));

    private static async Task<Created<SaleDto>> CreateAsync(SaveSaleRequest request, SaleService service, CancellationToken cancellationToken)
    {
        var sale = await service.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return TypedResults.Created($"/api/v1/sales/{sale.Id}", sale);
    }

    private static async Task<Ok<SaleDto>> UpdateAsync(long id, SaveSaleRequest request, SaleService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false));

    private static async Task<Ok<SaleDto>> ConfirmAsync(long id, ConfirmSaleRequest request, SaleService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ConfirmAsync(id, request, cancellationToken).ConfigureAwait(false));

    private static async Task<Ok<SaleDto>> CancelAsync(long id, CancelSaleRequest request, SaleService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.CancelAsync(id, request, cancellationToken).ConfigureAwait(false));
}
