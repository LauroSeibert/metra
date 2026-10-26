using Erp.Domain.Finance;

namespace Erp.Application.Finance;

public sealed record FinancialCategoryDto(long Id, string Name, FinancialCategoryKind Kind, int EntryCount, int Version);

public sealed record SaveFinancialCategoryRequest(string Name, FinancialCategoryKind Kind, int? Version);

/// <summary>Filtro de situação da listagem; <c>Overdue</c> é aberto com vencimento passado.</summary>
public enum FinancialEntryStatusFilter
{
    Open = 1,
    Overdue = 2,
    Paid = 3,
    Cancelled = 4,
}

/// <summary>Filtros da listagem. Vencimento entre <see cref="DueFrom"/> e <see cref="DueTo"/>, inclusive.</summary>
public sealed record FinancialEntryListQuery(
    FinancialEntryKind Kind,
    string? Search,
    FinancialEntryStatusFilter? Status,
    DateOnly? DueFrom,
    DateOnly? DueTo,
    long? CategoryId,
    long? PartnerId,
    string? Sort,
    int? Page,
    int? PageSize);

/// <summary>Lançamento na listagem e no detalhe. <see cref="DaysOverdue"/> é zero se não estiver vencido.</summary>
public sealed record FinancialEntryDto(
    long Id,
    FinancialEntryKind Kind,
    string Description,
    long CategoryId,
    string CategoryName,
    long? PartnerId,
    string? PartnerName,
    string? PartnerDocument,
    decimal Amount,
    DateOnly DueDate,
    FinancialEntryStatus Status,
    bool Overdue,
    int DaysOverdue,
    DateOnly? PaidAt,
    decimal? PaidAmount,
    int InstallmentNo,
    int InstallmentTotal,
    long? SaleId,
    DateTime? ReversedAt,
    DateTime? CancelledAt,
    string? ReversalReason,
    int Version,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Cartões de resumo da tela: quanto está aberto, vencido, a vencer em 7 dias e pago no mês.</summary>
public sealed record FinancialSummaryDto(
    FinancialEntryKind Kind,
    decimal OpenAmount,
    int OpenCount,
    decimal OverdueAmount,
    int OverdueCount,
    decimal DueNext7DaysAmount,
    int DueNext7DaysCount,
    decimal PaidThisMonthAmount,
    int PaidThisMonthCount);

/// <summary>
/// Nova conta. <see cref="Amount"/> é o total; com <see cref="Installments"/> maior que 1, vira uma
/// parcela por mês a partir de <see cref="DueDate"/>.
/// </summary>
public sealed record CreateFinancialEntryRequest(
    FinancialEntryKind Kind,
    string Description,
    long CategoryId,
    long? PartnerId,
    decimal Amount,
    DateOnly DueDate,
    int Installments);

public sealed record UpdateFinancialEntryRequest(
    string Description,
    long CategoryId,
    long? PartnerId,
    decimal Amount,
    DateOnly DueDate,
    int Version);

/// <summary>Baixa. Sem data, vale hoje; sem valor, o valor do lançamento.</summary>
public sealed record PayFinancialEntryRequest(DateOnly? PaidAt, decimal? PaidAmount, int Version);

public sealed record ReversePaymentRequest(string? Reason, int Version);

public sealed record CancelFinancialEntryRequest(int Version);
