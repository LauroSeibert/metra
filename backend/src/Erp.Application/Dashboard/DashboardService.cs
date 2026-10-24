using Erp.Application.Abstractions;
using Erp.Application.Common;
using Erp.Domain.Finance;
using Erp.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Dashboard;

public sealed record DashboardDto(
    decimal SalesThisMonth,
    int SalesThisMonthCount,
    decimal SalesLastMonth,
    int DraftCount,
    decimal ReceivableOpen,
    decimal ReceivableOverdue,
    decimal PayableOpen,
    decimal PayableOverdue,
    int LowStockCount,
    IReadOnlyList<DailySalesDto> DailySales,
    IReadOnlyList<TopProductDto> TopProducts,
    IReadOnlyList<RecentSaleDto> RecentSales);

public sealed record DailySalesDto(DateOnly Date, decimal Total, int Count);

public sealed record TopProductDto(long ProductId, string Name, decimal Quantity, decimal Total);

public sealed record RecentSaleDto(long Id, int Number, string PartnerName, SaleStatus Status, decimal Total, DateOnly IssuedAt);

/// <summary>Números da tela inicial: vendas do mês, contas em aberto, estoque baixo e últimos 30 dias.</summary>
public sealed class DashboardService(IErpDbContext db, BusinessCalendar calendar)
{
    private const int Days = 30;

    public async Task<DashboardDto> GetAsync(CancellationToken cancellationToken)
    {
        var today = calendar.Today;
        var monthStart = calendar.MonthStart;
        var lastMonthStart = monthStart.AddMonths(-1);
        var since = today.AddDays(-(Days - 1));

        var confirmed = db.Sales.AsNoTracking().Where(s => s.Status == SaleStatus.Confirmed);

        var thisMonth = await confirmed
            .Where(s => s.IssuedAt >= monthStart && s.IssuedAt <= today)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Sum(s => s.Total), Count = g.Count() })
            .FirstOrDefaultAsync(cancellationToken);
        var lastMonth = await confirmed
            .Where(s => s.IssuedAt >= lastMonthStart && s.IssuedAt < monthStart)
            .SumAsync(s => (decimal?)s.Total, cancellationToken) ?? 0;
        var drafts = await db.Sales.CountAsync(s => s.Status == SaleStatus.Draft, cancellationToken);

        var open = await db.FinancialEntries.AsNoTracking()
            .Where(e => e.Status == FinancialEntryStatus.Open)
            .GroupBy(e => new { e.Kind, Overdue = e.DueDate < today })
            .Select(g => new { g.Key.Kind, g.Key.Overdue, Amount = g.Sum(e => e.Amount) })
            .ToListAsync(cancellationToken);
        decimal OpenAmount(FinancialEntryKind kind, bool? overdue = null) =>
            open.Where(x => x.Kind == kind && (overdue is null || x.Overdue == overdue)).Sum(x => x.Amount);

        var lowStock = await db.StockBalances
            .Join(db.Products, b => b.ProductId, p => p.Id, (b, p) => new { b.Quantity, p.MinStock, p.Active })
            .CountAsync(x => x.Active && x.Quantity < x.MinStock, cancellationToken);

        var daily = await confirmed
            .Where(s => s.IssuedAt >= since && s.IssuedAt <= today)
            .GroupBy(s => s.IssuedAt)
            .Select(g => new { Date = g.Key, Total = g.Sum(s => s.Total), Count = g.Count() })
            .ToListAsync(cancellationToken);
        var dailySales = Enumerable.Range(0, Days)
            .Select(offset => since.AddDays(offset))
            .Select(date => daily.FirstOrDefault(d => d.Date == date) is { } day
                ? new DailySalesDto(date, day.Total, day.Count)
                : new DailySalesDto(date, 0, 0))
            .ToList();

        var top = await confirmed
            .Where(s => s.IssuedAt >= since && s.IssuedAt <= today)
            .SelectMany(s => s.Items)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity), Total = g.Sum(i => i.Total) })
            .OrderByDescending(x => x.Total)
            .Take(5)
            .ToListAsync(cancellationToken);
        var topIds = top.Select(x => x.ProductId).ToList();
        var names = await db.Products.AsNoTracking()
            .Where(p => topIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);
        var topProducts = top
            .Select(x => new TopProductDto(x.ProductId, names.GetValueOrDefault(x.ProductId, "—"), x.Quantity, x.Total))
            .ToList();

        var recent = await db.Sales.AsNoTracking()
            .OrderByDescending(s => s.Number)
            .Take(6)
            .Join(db.Partners, s => s.PartnerId, p => p.Id, (s, p) => new
            {
                s.Id,
                s.Number,
                PartnerName = p.Name,
                s.Status,
                s.Total,
                s.IssuedAt,
            })
            .ToListAsync(cancellationToken);

        return new DashboardDto(
            thisMonth?.Total ?? 0,
            thisMonth?.Count ?? 0,
            lastMonth,
            drafts,
            OpenAmount(FinancialEntryKind.Receivable),
            OpenAmount(FinancialEntryKind.Receivable, overdue: true),
            OpenAmount(FinancialEntryKind.Payable),
            OpenAmount(FinancialEntryKind.Payable, overdue: true),
            lowStock,
            dailySales,
            topProducts,
            recent.Select(s => new RecentSaleDto(s.Id, s.Number, s.PartnerName, s.Status, s.Total, s.IssuedAt)).ToList());
    }
}
