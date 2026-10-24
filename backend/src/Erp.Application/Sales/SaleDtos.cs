using Erp.Domain.Finance;
using Erp.Domain.Sales;

namespace Erp.Application.Sales;

/// <summary>Filtros da listagem. Período pela data da venda, inclusive nas duas pontas.</summary>
public sealed record SaleListQuery(
    int? Number,
    long? PartnerId,
    SaleStatus? Status,
    DateOnly? IssuedFrom,
    DateOnly? IssuedTo,
    string? Sort,
    int? Page,
    int? PageSize);

public sealed record SaleListItemDto(
    long Id,
    int Number,
    long PartnerId,
    string PartnerName,
    SaleStatus Status,
    PaymentMethod PaymentMethod,
    int Installments,
    int ItemCount,
    decimal Total,
    DateOnly IssuedAt,
    DateTime? ConfirmedAt);

public sealed record SaleItemDto(
    long Id,
    long ProductId,
    string Sku,
    string ProductName,
    string? UnitCode,
    decimal Quantity,
    decimal UnitPrice,
    decimal Discount,
    decimal Total);

/// <summary>Parcela a receber gerada pela venda (resumo para o detalhe).</summary>
public sealed record SaleReceivableDto(
    long Id,
    int InstallmentNo,
    int InstallmentTotal,
    decimal Amount,
    DateOnly DueDate,
    FinancialEntryStatus Status,
    DateOnly? PaidAt);

public sealed record SaleDto(
    long Id,
    int Number,
    long PartnerId,
    string PartnerName,
    string PartnerDocument,
    SaleStatus Status,
    PaymentMethod PaymentMethod,
    int Installments,
    decimal Gross,
    decimal Discount,
    decimal Subtotal,
    decimal Total,
    DateOnly IssuedAt,
    DateTime? ConfirmedAt,
    DateTime? CancelledAt,
    string? CancellationReason,
    string? Note,
    IReadOnlyList<SaleItemDto> Items,
    IReadOnlyList<SaleReceivableDto> Receivables,
    int Version,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Item enviado pela tela: o preço não vem do cliente, é copiado do produto.</summary>
public sealed record SaveSaleItemRequest(long ProductId, decimal Quantity, decimal Discount);

/// <summary>
/// Criação ou edição do rascunho. <see cref="Version"/> é obrigatória na edição. Sem
/// <see cref="IssuedAt"/>, a venda é de hoje.
/// </summary>
public sealed record SaveSaleRequest(
    long PartnerId,
    PaymentMethod PaymentMethod,
    int Installments,
    decimal Discount,
    DateOnly? IssuedAt,
    string? Note,
    IReadOnlyList<SaveSaleItemRequest> Items,
    int? Version);

public sealed record ConfirmSaleRequest(int Version);

public sealed record CancelSaleRequest(string? Reason, int Version);

/// <summary>Configuração que a tela de venda precisa saber antes de enviar.</summary>
public sealed record SaleSettingsDto(decimal MaxDiscountPercent, int MaxInstallments);
