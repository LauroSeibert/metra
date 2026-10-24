using Erp.Application.Abstractions;
using Erp.Application.Common;
using Erp.Application.Settings;
using Erp.Application.Stock;
using Erp.Domain.Common;
using Erp.Domain.Finance;
using Erp.Domain.Partners;
using Erp.Domain.Sales;
using Erp.Domain.Settings;
using Erp.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Sales;

/// <summary>
/// Vendas. Confirmar baixa o estoque e gera as parcelas a receber; cancelar a confirmada estorna os
/// dois. Cada uma dessas operações roda numa única transação; conflito de versão (saldo, sequência)
/// refaz a operação inteira.
/// </summary>
public sealed class SaleService(
    IErpDbContext db,
    StockService stock,
    SettingsReader settings,
    ICurrentUser currentUser,
    BusinessCalendar calendar)
{
    public static readonly IReadOnlyCollection<string> SortFields = ["number", "issuedAt", "total"];
    /// <summary>
    /// Tentativas em conflito de versão. A sequência de números é disputada por toda venda nova, então
    /// há folga para picos de vendas simultâneas; entre tentativas, uma espera curta e aleatória.
    /// </summary>
    private const int MaxAttempts = 10;

    public async Task<PagedResult<SaleListItemDto>> ListAsync(SaleListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var page = PageRequest.From(query.Page, query.PageSize);
        // Padrão: as vendas mais recentes (número maior) primeiro.
        var sort = SortRequest.Parse(string.IsNullOrWhiteSpace(query.Sort) ? "-number" : query.Sort, "number", SortFields);

        var sales = db.Sales.AsNoTracking();
        if (query.Number is { } number)
        {
            sales = sales.Where(s => s.Number == number);
        }

        if (query.PartnerId is { } partnerId)
        {
            sales = sales.Where(s => s.PartnerId == partnerId);
        }

        if (query.Status is { } status)
        {
            sales = sales.Where(s => s.Status == status);
        }

        if (query.IssuedFrom is { } from)
        {
            sales = sales.Where(s => s.IssuedAt >= from);
        }

        if (query.IssuedTo is { } to)
        {
            sales = sales.Where(s => s.IssuedAt <= to);
        }

        sales = (sort.Field, sort.Descending) switch
        {
            ("issuedAt", false) => sales.OrderBy(s => s.IssuedAt).ThenBy(s => s.Number),
            ("issuedAt", true) => sales.OrderByDescending(s => s.IssuedAt).ThenByDescending(s => s.Number),
            ("total", false) => sales.OrderBy(s => s.Total).ThenBy(s => s.Number),
            ("total", true) => sales.OrderByDescending(s => s.Total).ThenByDescending(s => s.Number),
            (_, false) => sales.OrderBy(s => s.Number),
            _ => sales.OrderByDescending(s => s.Number),
        };

        // Conta e pagina só em `sale`; cliente e quantidade de itens só para os da página (ver finanças).
        var total = await sales.CountAsync(cancellationToken).ConfigureAwait(false);
        var pageIds = await sales.Skip(page.Skip).Take(page.PageSize).Select(s => s.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var rows = await (
                from s in db.Sales.AsNoTracking().Where(s => pageIds.Contains(s.Id))
                join partner in db.Partners on s.PartnerId equals partner.Id
                select new SaleListItemDto(
                    s.Id,
                    s.Number,
                    s.PartnerId,
                    partner.Name,
                    s.Status,
                    s.PaymentMethod,
                    s.Installments,
                    s.Items.Count,
                    s.Total,
                    s.IssuedAt,
                    s.ConfirmedAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var position = pageIds.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        return new PagedResult<SaleListItemDto>([.. rows.OrderBy(r => position[r.Id])], page.Page, page.PageSize, total);
    }

    public async Task<SaleDto> GetAsync(long id, CancellationToken cancellationToken)
    {
        var sale = await db.Sales.AsNoTracking().Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false)
            ?? throw NotFound();
        var partner = await db.Partners.AsNoTracking()
            .Where(p => p.Id == sale.PartnerId)
            .Select(p => new { p.Name, p.Document })
            .FirstAsync(cancellationToken)
            .ConfigureAwait(false);
        var productIds = sale.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Sku, p.Name, UnitCode = p.Unit == null ? null : p.Unit.Code })
            .ToDictionaryAsync(p => p.Id, cancellationToken)
            .ConfigureAwait(false);
        var receivables = await db.FinancialEntries.AsNoTracking()
            .Where(e => e.SaleId == id)
            .OrderBy(e => e.InstallmentNo)
            .Select(e => new SaleReceivableDto(e.Id, e.InstallmentNo, e.InstallmentTotal, e.Amount, e.DueDate, e.Status, e.PaidAt))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new SaleDto(
            sale.Id,
            sale.Number,
            sale.PartnerId,
            partner.Name,
            partner.Document,
            sale.Status,
            sale.PaymentMethod,
            sale.Installments,
            sale.Gross,
            sale.Discount,
            sale.Subtotal,
            sale.Total,
            sale.IssuedAt,
            sale.ConfirmedAt,
            sale.CancelledAt,
            sale.CancellationReason,
            sale.Note,
            [.. sale.Items.OrderBy(i => i.Id).Select(i => new SaleItemDto(
                i.Id,
                i.ProductId,
                products[i.ProductId].Sku,
                products[i.ProductId].Name,
                products[i.ProductId].UnitCode,
                i.Quantity,
                i.UnitPrice,
                i.Discount,
                i.Total))],
            receivables,
            sale.Version,
            sale.CreatedAt,
            sale.UpdatedAt);
    }

    public async Task<SaleSettingsDto> GetSettingsAsync(CancellationToken cancellationToken) =>
        new(await settings.SalesMaxDiscountPercentAsync(cancellationToken).ConfigureAwait(false), Sale.MaxInstallments);

    /// <summary>Cria o rascunho com o próximo número da sequência.</summary>
    public async Task<SaleDto> CreateAsync(SaveSaleRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var maxDiscount = await settings.SalesMaxDiscountPercentAsync(cancellationToken).ConfigureAwait(false);
        var (data, items) = await PrepareAsync(request, cancellationToken).ConfigureAwait(false);

        var id = await RetryAsync(
            async () =>
            {
                var sequence = await db.DocumentSequences.FirstOrDefaultAsync(s => s.Name == DocumentSequence.Sale, cancellationToken).ConfigureAwait(false);
                if (sequence is null)
                {
                    sequence = DocumentSequence.Create(DocumentSequence.Sale);
                    db.DocumentSequences.Add(sequence);
                }

                var number = checked((int)sequence.Next());
                // Grava a sequência antes da venda: o UPDATE trava a linha e serializa vendas simultâneas
                // (com tudo num SaveChanges só, o INSERT ia primeiro e duas vendas disputavam o mesmo número).
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                var sale = Sale.CreateDraft(number, data, items, maxDiscount);
                db.Sales.Add(sale);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return sale.Id;
            },
            cancellationToken).ConfigureAwait(false);

        return await GetAsync(id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<SaleDto> UpdateAsync(long id, SaveSaleRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var sale = await FindTrackedAsync(id, request.Version ?? -1, cancellationToken).ConfigureAwait(false);
        var maxDiscount = await settings.SalesMaxDiscountPercentAsync(cancellationToken).ConfigureAwait(false);
        var (data, items) = await PrepareAsync(request, cancellationToken).ConfigureAwait(false);

        sale.UpdateDraft(data, items, maxDiscount);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetAsync(id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Confirma: baixa o estoque de cada item e gera as parcelas a receber, numa transação só.</summary>
    public async Task<SaleDto> ConfirmAsync(long id, ConfirmSaleRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var maxDiscount = await settings.SalesMaxDiscountPercentAsync(cancellationToken).ConfigureAwait(false);
        var allowNegative = await settings.StockAllowNegativeAsync(cancellationToken).ConfigureAwait(false);
        var categoryId = await RevenueCategoryAsync(cancellationToken).ConfigureAwait(false);
        var userId = currentUser.Id;

        await RetryAsync(
            async () =>
            {
                var sale = await FindTrackedAsync(id, request.Version, cancellationToken).ConfigureAwait(false);
                await EnsureProductsActiveAsync(sale.Items.Select(i => i.ProductId), cancellationToken).ConfigureAwait(false);
                var now = calendar.UtcNow;
                var today = calendar.Today;
                sale.Confirm(now, maxDiscount);

                foreach (var item in sale.Items)
                {
                    var movement = StockMovement.ForSale(item.ProductId, item.Quantity, now, userId, sale.Id, sale.Number);
                    await stock.ApplyWithinTransactionAsync(movement, allowNegative, cancellationToken).ConfigureAwait(false);
                }

                // À vista: uma parcela já recebida na confirmação. A prazo: parcelas mensais, a primeira
                // um mês depois da data da venda.
                var immediate = Sale.IsImmediate(sale.PaymentMethod);
                var entries = FinancialEntry.CreateInstallments(
                    new FinancialEntryData(
                        FinancialEntryKind.Receivable,
                        $"Venda {sale.Number}",
                        categoryId,
                        sale.PartnerId,
                        sale.Total,
                        immediate ? sale.IssuedAt : sale.IssuedAt.AddMonths(1)),
                    sale.Installments);
                foreach (var entry in entries)
                {
                    entry.AttachToSale(sale.Id);
                    if (immediate)
                    {
                        entry.Pay(today < entry.DueDate ? today : entry.DueDate, entry.Amount, today);
                    }
                }

                db.FinancialEntries.AddRange(entries);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return sale.Id;
            },
            cancellationToken).ConfigureAwait(false);

        return await GetAsync(id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Cancela. Rascunho: só muda a situação. Confirmada: devolve o estoque de cada item, estorna a baixa
    /// das parcelas recebidas e cancela todas, na mesma transação.
    /// </summary>
    public async Task<SaleDto> CancelAsync(long id, CancelSaleRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var userId = currentUser.Id;

        await RetryAsync(
            async () =>
            {
                var sale = await FindTrackedAsync(id, request.Version, cancellationToken).ConfigureAwait(false);
                var wasConfirmed = sale.Status == SaleStatus.Confirmed;
                var now = calendar.UtcNow;
                sale.Cancel(now, request.Reason);

                if (wasConfirmed)
                {
                    foreach (var item in sale.Items)
                    {
                        var movement = StockMovement.ForSaleCancellation(item.ProductId, item.Quantity, now, userId, sale.Id, sale.Number);
                        await stock.ApplyWithinTransactionAsync(movement, allowNegative: true, cancellationToken).ConfigureAwait(false);
                    }

                    var entries = await db.FinancialEntries.Where(e => e.SaleId == sale.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
                    foreach (var entry in entries)
                    {
                        if (entry.Status == FinancialEntryStatus.Paid)
                        {
                            entry.ReversePayment($"Venda {sale.Number} cancelada", now);
                        }

                        if (entry.Status == FinancialEntryStatus.Open)
                        {
                            entry.Cancel(now);
                        }
                    }
                }

                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return sale.Id;
            },
            cancellationToken).ConfigureAwait(false);

        return await GetAsync(id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Roda a operação numa transação. Se outra gravação mudou um saldo ou a sequência no meio (conflito
    /// de versão), descarta tudo e refaz; conflito na própria venda vira 409 (versão desatualizada).
    /// </summary>
    private async Task<long> RetryAsync(Func<Task<long>> operation, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                try
                {
                    var id = await operation().ConfigureAwait(false);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return id;
                }
                catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
                {
                    db.ChangeTracker.Clear();
                    await Task.Delay(Random.Shared.Next(5, 20 * attempt), cancellationToken).ConfigureAwait(false);
                }
                catch
                {
                    db.ChangeTracker.Clear();
                    throw;
                }
            }
        }
    }

    /// <summary>Cliente válido e preço de cada item copiado do produto, que precisa estar ativo.</summary>
    private async Task<(SaleData Data, List<SaleItemData> Items)> PrepareAsync(SaveSaleRequest request, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();
        var partner = await db.Partners.AsNoTracking()
            .Where(p => p.Id == request.PartnerId)
            .Select(p => new { p.Type, p.Active })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (partner is null || !partner.Active)
        {
            errors.Add("partnerId", "Cliente não encontrado ou inativo.");
        }
        else if (partner.Type == PartnerType.Supplier)
        {
            errors.Add("partnerId", "O parceiro é só fornecedor; marque-o como cliente para vender.");
        }

        var requested = request.Items ?? [];
        var productIds = requested.Select(i => i.ProductId).Distinct().ToList();
        var products = await db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Sku, p.SalePrice, p.Active })
            .ToDictionaryAsync(p => p.Id, cancellationToken)
            .ConfigureAwait(false);
        foreach (var item in requested)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
            {
                errors.Add("items", "Produto não encontrado.");
            }
            else if (!product.Active)
            {
                errors.Add("items", $"O produto {product.Sku} está inativo e não pode ser vendido.");
            }
        }

        errors.ThrowIfAny();
        var data = new SaleData(request.PartnerId, request.PaymentMethod, request.Installments, request.Discount, request.IssuedAt ?? calendar.Today, request.Note);
        var items = requested.Select(i => new SaleItemData(i.ProductId, i.Quantity, products[i.ProductId].SalePrice, i.Discount)).ToList();
        return (data, items);
    }

    private async Task EnsureProductsActiveAsync(IEnumerable<long> productIds, CancellationToken cancellationToken)
    {
        var ids = productIds.Distinct().ToList();
        var inactive = await db.Products.AsNoTracking()
            .Where(p => ids.Contains(p.Id) && !p.Active)
            .Select(p => p.Sku)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        if (inactive.Count > 0)
        {
            new ValidationErrors().Add("items", $"Produtos inativos não são vendidos: {string.Join(", ", inactive)}.").ThrowIfAny();
        }
    }

    /// <summary>Categoria das parcelas: a da configuração ou, sem ela, a categoria de receita "Vendas".</summary>
    private async Task<long> RevenueCategoryAsync(CancellationToken cancellationToken)
    {
        var configured = await settings.GetLongAsync(SettingKeys.SalesRevenueCategoryId, cancellationToken).ConfigureAwait(false);
        var category = await db.FinancialCategories.AsNoTracking()
            .Where(c => c.Kind == FinancialCategoryKind.Revenue && (configured == null ? c.Name == "Vendas" : c.Id == configured))
            .Select(c => (long?)c.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return category ?? throw new InvalidStateException(
            "Não há categoria de receita para as vendas: crie a categoria \"Vendas\" ou defina-a nas configurações.");
    }

    private async Task<Sale> FindTrackedAsync(long id, int version, CancellationToken cancellationToken)
    {
        var sale = await db.Sales.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == id, cancellationToken).ConfigureAwait(false)
            ?? throw NotFound();
        return sale.Version == version ? sale : throw ConflictException.StaleVersion();
    }

    private static NotFoundException NotFound() => new(ErrorCodes.SaleNotFound, "Venda não encontrada.");
}
