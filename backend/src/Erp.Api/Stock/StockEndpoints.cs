using Erp.Api.Access;
using Erp.Application.Common;
using Erp.Application.Stock;
using Erp.Domain.Stock;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Erp.Api.Stock;

/// <summary>Saldo por produto, histórico (paginação por chave), lançamento manual e estorno.</summary>
internal static class StockEndpoints
{
    public static RouteGroupBuilder MapStockEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/stock-balances", ListBalancesAsync)
            .WithTags("Stock")
            .WithName("ListStockBalances")
            .WithSummary("Saldo por produto, com busca por prefixo de SKU ou nome e filtro de estoque baixo.")
            .ProducesValidationProblem();

        var movements = api.MapGroup("/stock-movements").WithTags("Stock");

        movements.MapGet("/", ListMovementsAsync)
            .WithName("ListStockMovements")
            .WithSummary("Histórico do mais recente para o mais antigo, paginado por chave (cursor).")
            .ProducesValidationProblem();

        movements.MapGet("/{id:long}", GetMovementAsync)
            .WithName("GetStockMovement")
            .ProducesProblem(StatusCodes.Status404NotFound);

        movements.MapPost("/", RecordAsync)
            .WithName("CreateStockMovement")
            .WithSummary("Lançamento manual de entrada, saída ou ajuste de inventário.")
            .RequireWrite()
            .ProducesValidationProblem();

        movements.MapPost("/{id:long}/reverse", ReverseAsync)
            .WithName("ReverseStockMovement")
            .WithSummary("Estorna um movimento uma única vez, com a quantidade oposta.")
            .RequireWrite()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }

    private static async Task<Ok<PagedResult<StockBalanceDto>>> ListBalancesAsync(
        StockService service,
        string? search,
        long? categoryId,
        bool? lowStock,
        string? sort,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service
            .ListBalancesAsync(new StockBalanceListQuery(search, categoryId, lowStock, sort, page, pageSize), cancellationToken)
            .ConfigureAwait(false));

    private static async Task<Ok<KeysetPage<StockMovementDto>>> ListMovementsAsync(
        StockService service,
        long? productId,
        StockMovementType? type,
        DateTime? from,
        DateTime? to,
        string? cursor,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service
            .ListMovementsAsync(new StockMovementQuery(productId, type, from, to, cursor, pageSize), cancellationToken)
            .ConfigureAwait(false));

    private static async Task<Ok<StockMovementDto>> GetMovementAsync(long id, StockService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetMovementAsync(id, cancellationToken).ConfigureAwait(false));

    private static async Task<Created<StockMovementDto>> RecordAsync(
        CreateStockMovementRequest request,
        StockService service,
        CancellationToken cancellationToken)
    {
        var movement = await service.RecordAsync(request, cancellationToken).ConfigureAwait(false);
        return TypedResults.Created($"/api/v1/stock-movements/{movement.Id}", movement);
    }

    private static async Task<Created<StockMovementDto>> ReverseAsync(
        long id,
        ReverseStockMovementRequest request,
        StockService service,
        CancellationToken cancellationToken)
    {
        var reversal = await service.ReverseAsync(id, request, cancellationToken).ConfigureAwait(false);
        return TypedResults.Created($"/api/v1/stock-movements/{reversal.Id}", reversal);
    }
}
