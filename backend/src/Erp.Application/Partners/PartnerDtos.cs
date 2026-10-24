using Erp.Domain.Partners;

namespace Erp.Application.Partners;

/// <summary>
/// Filtros da listagem de parceiros. <see cref="Type"/> <c>Customer</c> ou <c>Supplier</c> inclui
/// também os parceiros <c>Both</c>.
/// </summary>
public sealed record PartnerListQuery(
    string? Search,
    PartnerType? Type,
    bool? Active,
    string? Sort,
    int? Page,
    int? PageSize);

public sealed record PartnerListItemDto(
    long Id,
    PartnerType Type,
    string Name,
    string Document,
    TaxDocumentKind DocumentKind,
    string? Email,
    string? Phone,
    bool Active,
    DateTime UpdatedAt);

public sealed record PartnerDto(
    long Id,
    PartnerType Type,
    string Name,
    string Document,
    TaxDocumentKind DocumentKind,
    string? Email,
    string? Phone,
    bool Active,
    int Version,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Criação de parceiro. O documento pode vir formatado (000.000.000-00); é gravado normalizado.</summary>
public sealed record CreatePartnerRequest(
    PartnerType Type,
    string Name,
    string Document,
    string? Email,
    string? Phone);

/// <summary>Edição de parceiro. <see cref="Version"/> é a versão lida pelo cliente (concorrência otimista).</summary>
public sealed record UpdatePartnerRequest(
    PartnerType Type,
    string Name,
    string Document,
    string? Email,
    string? Phone,
    int Version);
