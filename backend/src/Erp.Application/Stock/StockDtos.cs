using Erp.Domain.Stock;

namespace Erp.Application.Stock;

/// <summary>Filtros do saldo por produto. <see cref="LowStock"/>: saldo abaixo do estoque mínimo.</summary>
public sealed record StockBalanceListQuery(
    string? Search,
    long? CategoryId,
    bool? LowStock,
    string? Sort,
    int? Page,
    int? PageSize);

public sealed record StockBalanceDto(
    long ProductId,
    string Sku,
    string Name,
    string? CategoryName,
    string? UnitCode,
    decimal Quantity,
    decimal MinStock,
    bool LowStock,
    bool Active);

/// <summary>
/// Filtros do histórico, paginado por chave: <see cref="Cursor"/> é o <c>nextCursor</c> da página
/// anterior (vazio na primeira). Período em UTC, <see cref="To"/> exclusivo.
/// </summary>
public sealed record StockMovementQuery(
    long? ProductId,
    StockMovementType? Type,
    DateTime? From,
    DateTime? To,
    string? Cursor,
    int? PageSize);

/// <summary>Movimentação no histórico.</summary>
/// <param name="ReversedById">Id do estorno deste movimento, se já foi estornado.</param>
public sealed record StockMovementDto(
    long Id,
    long ProductId,
    string Sku,
    string ProductName,
    string? UnitCode,
    StockMovementType Type,
    decimal Quantity,
    decimal? UnitCost,
    DateTime OccurredAt,
    string? ReferenceType,
    long? ReferenceId,
    long UserId,
    string? UserName,
    string? Note,
    long? ReversedById,
    DateTime CreatedAt);

/// <summary>Página por chave: sem total (contar 1 milhão de linhas custa caro); <c>nextCursor</c> nulo no fim.</summary>
public sealed record KeysetPage<T>(IReadOnlyList<T> Items, string? NextCursor);

/// <summary>
/// Lançamento manual. <see cref="Quantity"/> é positiva na entrada e na saída; no ajuste é a
/// contagem (o novo saldo), e o sistema grava a diferença.
/// </summary>
public sealed record CreateStockMovementRequest(
    long ProductId,
    StockMovementType Type,
    decimal Quantity,
    decimal? UnitCost,
    DateTime? OccurredAt,
    string? Note);

public sealed record ReverseStockMovementRequest(string? Note);
