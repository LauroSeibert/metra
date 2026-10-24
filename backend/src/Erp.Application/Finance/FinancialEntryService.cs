using Erp.Application.Abstractions;
using Erp.Application.Common;
using Erp.Domain.Common;
using Erp.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Finance;

/// <summary>Contas a receber e a pagar: listagem com resumo, parcelamento, baixa, estorno e cancelamento.</summary>
public sealed class FinancialEntryService(IErpDbContext db, BusinessCalendar calendar)
{
    public static readonly IReadOnlyCollection<string> SortFields = ["dueDate", "amount", "description", "createdAt"];

    public async Task<PagedResult<FinancialEntryDto>> ListAsync(FinancialEntryListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var page = PageRequest.From(query.Page, query.PageSize);
        var sort = SortRequest.Parse(query.Sort, "dueDate", SortFields);
        var today = calendar.Today;

        var entries = db.FinancialEntries.AsNoTracking().Where(e => e.Kind == query.Kind);
        entries = query.Status switch
        {
            FinancialEntryStatusFilter.Open => entries.Where(e => e.Status == FinancialEntryStatus.Open),
            FinancialEntryStatusFilter.Overdue => entries.Where(e => e.Status == FinancialEntryStatus.Open && e.DueDate < today),
            FinancialEntryStatusFilter.Paid => entries.Where(e => e.Status == FinancialEntryStatus.Paid),
            FinancialEntryStatusFilter.Cancelled => entries.Where(e => e.Status == FinancialEntryStatus.Cancelled),
            _ => entries,
        };

        if (query.DueFrom is { } from)
        {
            entries = entries.Where(e => e.DueDate >= from);
        }

        if (query.DueTo is { } to)
        {
            entries = entries.Where(e => e.DueDate <= to);
        }

        if (query.CategoryId is { } categoryId)
        {
            entries = entries.Where(e => e.CategoryId == categoryId);
        }

        if (query.PartnerId is { } partnerId)
        {
            entries = entries.Where(e => e.PartnerId == partnerId);
        }

        var pattern = SearchPattern.Prefix(query.Search);
        if (pattern is not null)
        {
            entries = entries.Where(e => EF.Functions.Like(e.Description, pattern, SearchPattern.EscapeCharacter));
        }

        entries = (sort.Field, sort.Descending) switch
        {
            ("amount", false) => entries.OrderBy(e => e.Amount).ThenBy(e => e.Id),
            ("amount", true) => entries.OrderByDescending(e => e.Amount).ThenByDescending(e => e.Id),
            ("description", false) => entries.OrderBy(e => e.Description).ThenBy(e => e.Id),
            ("description", true) => entries.OrderByDescending(e => e.Description).ThenByDescending(e => e.Id),
            ("createdAt", false) => entries.OrderBy(e => e.CreatedAt).ThenBy(e => e.Id),
            ("createdAt", true) => entries.OrderByDescending(e => e.CreatedAt).ThenByDescending(e => e.Id),
            (_, true) => entries.OrderByDescending(e => e.DueDate).ThenByDescending(e => e.Id),
            _ => entries.OrderBy(e => e.DueDate).ThenBy(e => e.Id),
        };

        // Conta e pagina só na tabela de lançamentos e depois junta categoria e parceiro dos 20 da página:
        // contar com os joins custava 215 ms contra 6 ms (EXPLAIN em docs/modules/finance.md).
        var total = await entries.CountAsync(cancellationToken).ConfigureAwait(false);
        var pageIds = await entries.Skip(page.Skip).Take(page.PageSize).Select(e => e.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var rows = await Project(db.FinancialEntries.AsNoTracking().Where(e => pageIds.Contains(e.Id)), today)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var position = pageIds.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        var items = rows.OrderBy(row => position[row.Id]).Select(row => WithDaysOverdue(row, today)).ToList();
        return new PagedResult<FinancialEntryDto>(items, page.Page, page.PageSize, total);
    }

    public async Task<FinancialSummaryDto> SummaryAsync(FinancialEntryKind kind, CancellationToken cancellationToken)
    {
        var today = calendar.Today;
        var nextWeek = today.AddDays(7);
        var monthStart = calendar.MonthStart;
        var entries = db.FinancialEntries.AsNoTracking().Where(e => e.Kind == kind);
        var open = entries.Where(e => e.Status == FinancialEntryStatus.Open);

        var openTotals = await Totals(open, cancellationToken).ConfigureAwait(false);
        var overdue = await Totals(open.Where(e => e.DueDate < today), cancellationToken).ConfigureAwait(false);
        var dueSoon = await Totals(open.Where(e => e.DueDate >= today && e.DueDate <= nextWeek), cancellationToken).ConfigureAwait(false);
        var paid = await entries
            .Where(e => e.Status == FinancialEntryStatus.Paid && e.PaidAt >= monthStart)
            .GroupBy(_ => 1)
            .Select(g => new { Amount = g.Sum(e => e.PaidAmount ?? 0), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return new FinancialSummaryDto(
            kind,
            openTotals.Amount,
            openTotals.Count,
            overdue.Amount,
            overdue.Count,
            dueSoon.Amount,
            dueSoon.Count,
            paid?.Amount ?? 0,
            paid?.Count ?? 0);
    }

    public async Task<FinancialEntryDto> GetAsync(long id, CancellationToken cancellationToken)
    {
        var today = calendar.Today;
        var entry = await Project(db.FinancialEntries.AsNoTracking().Where(e => e.Id == id), today)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return entry is null ? throw NotFound() : WithDaysOverdue(entry, today);
    }

    /// <summary>Cria a conta, já dividida em parcelas. Devolve as parcelas criadas.</summary>
    public async Task<IReadOnlyList<FinancialEntryDto>> CreateAsync(CreateFinancialEntryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var data = new FinancialEntryData(request.Kind, request.Description, request.CategoryId, request.PartnerId, request.Amount, request.DueDate);
        var entries = FinancialEntry.CreateInstallments(data, request.Installments);
        await EnsureReferencesAsync(data, cancellationToken).ConfigureAwait(false);

        db.FinancialEntries.AddRange(entries);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var ids = entries.Select(e => e.Id).ToList();
        var today = calendar.Today;
        var created = await Project(db.FinancialEntries.AsNoTracking().Where(e => ids.Contains(e.Id)).OrderBy(e => e.InstallmentNo), today)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return [.. created.Select(e => WithDaysOverdue(e, today))];
    }

    public async Task<FinancialEntryDto> UpdateAsync(long id, UpdateFinancialEntryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entry = await FindTrackedAsync(id, request.Version, cancellationToken).ConfigureAwait(false);
        var data = new FinancialEntryData(entry.Kind, request.Description, request.CategoryId, request.PartnerId, request.Amount, request.DueDate);
        entry.Update(data);
        await EnsureReferencesAsync(data, cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FinancialEntryDto> PayAsync(long id, PayFinancialEntryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entry = await FindTrackedAsync(id, request.Version, cancellationToken).ConfigureAwait(false);
        var today = calendar.Today;
        entry.Pay(request.PaidAt ?? today, request.PaidAmount ?? entry.Amount, today);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FinancialEntryDto> ReversePaymentAsync(long id, ReversePaymentRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entry = await FindTrackedAsync(id, request.Version, cancellationToken).ConfigureAwait(false);
        entry.ReversePayment(request.Reason, calendar.UtcNow);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FinancialEntryDto> CancelAsync(long id, CancelFinancialEntryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var entry = await FindTrackedAsync(id, request.Version, cancellationToken).ConfigureAwait(false);
        entry.Cancel(calendar.UtcNow);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetAsync(id, cancellationToken).ConfigureAwait(false);
    }

    private IQueryable<FinancialEntryDto> Project(IQueryable<FinancialEntry> entries, DateOnly today) =>
        from e in entries
        join category in db.FinancialCategories on e.CategoryId equals category.Id
        join partner in db.Partners on e.PartnerId equals partner.Id into partners
        from partner in partners.DefaultIfEmpty()
        select new FinancialEntryDto(
            e.Id,
            e.Kind,
            e.Description,
            e.CategoryId,
            category.Name,
            e.PartnerId,
            partner == null ? null : partner.Name,
            e.Amount,
            e.DueDate,
            e.Status,
            e.Status == FinancialEntryStatus.Open && e.DueDate < today,
            0,
            e.PaidAt,
            e.PaidAmount,
            e.InstallmentNo,
            e.InstallmentTotal,
            e.SaleId,
            e.ReversedAt,
            e.ReversalReason,
            e.Version,
            e.CreatedAt,
            e.UpdatedAt);

    /// <summary>Dias de atraso calculados depois da consulta (aritmética de datas não vai ao SQL).</summary>
    private static FinancialEntryDto WithDaysOverdue(FinancialEntryDto entry, DateOnly today) =>
        entry.Overdue ? entry with { DaysOverdue = today.DayNumber - entry.DueDate.DayNumber } : entry;

    private static async Task<(decimal Amount, int Count)> Totals(IQueryable<FinancialEntry> entries, CancellationToken cancellationToken)
    {
        var totals = await entries
            .GroupBy(_ => 1)
            .Select(g => new { Amount = g.Sum(e => e.Amount), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return (totals?.Amount ?? 0, totals?.Count ?? 0);
    }

    private async Task EnsureReferencesAsync(FinancialEntryData data, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();
        var category = await db.FinancialCategories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == data.CategoryId, cancellationToken)
            .ConfigureAwait(false);
        if (category is null)
        {
            errors.Add("categoryId", "Categoria não encontrada.");
        }
        else if (!category.Fits(data.Kind))
        {
            errors.Add("categoryId", data.Kind == FinancialEntryKind.Receivable
                ? "Conta a receber usa categoria de receita."
                : "Conta a pagar usa categoria de despesa.");
        }

        if (data.PartnerId is { } partnerId
            && !await db.Partners.AnyAsync(p => p.Id == partnerId, cancellationToken).ConfigureAwait(false))
        {
            errors.Add("partnerId", "Parceiro não encontrado.");
        }

        errors.ThrowIfAny();
    }

    private async Task<FinancialEntry> FindTrackedAsync(long id, int version, CancellationToken cancellationToken)
    {
        var entry = await db.FinancialEntries.FirstOrDefaultAsync(e => e.Id == id, cancellationToken).ConfigureAwait(false)
            ?? throw NotFound();
        return entry.Version == version ? entry : throw ConflictException.StaleVersion();
    }

    private static NotFoundException NotFound() => new(ErrorCodes.FinancialEntryNotFound, "Lançamento não encontrado.");
}
