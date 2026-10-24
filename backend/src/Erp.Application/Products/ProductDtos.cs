namespace Erp.Application.Products;

/// <summary>Filtros da listagem de produtos. <see cref="LowStock"/>: saldo abaixo do estoque mínimo.</summary>
public sealed record ProductListQuery(
    string? Search,
    long? CategoryId,
    bool? Active,
    bool? LowStock,
    string? Sort,
    int? Page,
    int? PageSize);

public sealed record ProductListItemDto(
    long Id,
    string Sku,
    string Name,
    string? Description,
    long? CategoryId,
    string? CategoryName,
    long? UnitId,
    string? UnitCode,
    decimal CostPrice,
    decimal SalePrice,
    decimal MinStock,
    decimal StockQuantity,
    bool Active,
    DateTime UpdatedAt);

public sealed record ProductDto(
    long Id,
    string Sku,
    string Name,
    string? Description,
    long? CategoryId,
    string? CategoryName,
    long? UnitId,
    string? UnitCode,
    string? UnitName,
    decimal CostPrice,
    decimal SalePrice,
    decimal MinStock,
    bool Active,
    int Version,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Criação de produto. <see cref="Sku"/> vazio ou nulo: o servidor gera (prefixo da categoria + id).</summary>
public sealed record CreateProductRequest(
    string? Sku,
    string Name,
    string? Description,
    long? CategoryId,
    long? UnitId,
    decimal CostPrice,
    decimal SalePrice,
    decimal MinStock);

/// <summary>
/// Edição de produto. <see cref="Version"/> é a versão lida pelo cliente (concorrência otimista);
/// <see cref="Sku"/> vazio gera um novo, como na criação.
/// </summary>
public sealed record UpdateProductRequest(
    string? Sku,
    string Name,
    string? Description,
    long? CategoryId,
    long? UnitId,
    decimal CostPrice,
    decimal SalePrice,
    decimal MinStock,
    int Version);

public sealed record ProductCategoryDto(long Id, string Name, int ProductCount, int Version);

public sealed record SaveProductCategoryRequest(string Name, int? Version);

public sealed record UnitDto(long Id, string Code, string Name, int ProductCount, int Version);

public sealed record SaveUnitRequest(string Code, string Name, int? Version);
