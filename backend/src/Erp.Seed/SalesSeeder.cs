using System.Diagnostics;
using Erp.Domain.Finance;
using Erp.Domain.Partners;
using Erp.Domain.Sales;
using Erp.Domain.Stock;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Seed;

/// <summary>
/// Vendas sintéticas nos últimos dois anos (10 mil no perfil full por enquanto; o plano prevê 200 mil): 85% confirmadas, 5% canceladas ainda
/// em rascunho e 10% de rascunhos recentes. Cada confirmada gera o que a API geraria: saída de estoque por
/// item (tipo Sale) e parcelas a receber ligadas à venda (à vista já recebida; a prazo, vencidas quase todas
/// recebidas). Produtos que ficariam com saldo negativo recebem uma entrada de reposição no início do
/// período, e os saldos são recalculados como a soma das movimentações.
/// </summary>
internal sealed class SalesSeeder(Func<ErpDbContext> createContext, int count, int seed)
{
    private const int Batch = 5_000;
    private static readonly PaymentMethod[] Methods = [PaymentMethod.Cash, PaymentMethod.Pix, PaymentMethod.DebitCard, PaymentMethod.CreditCard, PaymentMethod.BankSlip];

    private static readonly string[] SaleColumns =
        ["number", "partner_id", "status", "payment_method", "installments", "discount", "subtotal", "total", "issued_at", "confirmed_at", "created_at", "updated_at", "version"];

    private static readonly string[] ItemColumns =
        ["sale_id", "product_id", "quantity", "unit_price", "discount", "total", "created_at", "updated_at", "version"];

    private static readonly string[] MovementColumns =
        ["product_id", "type", "quantity", "occurred_at", "reference_type", "reference_id", "user_id", "note", "created_at", "updated_at", "version"];

    private static readonly string[] EntryColumns =
        ["kind", "category_id", "partner_id", "sale_id", "description", "amount", "due_date", "status", "paid_at", "paid_amount", "installment_no", "installment_total", "created_at", "updated_at", "version"];

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        await using var db = createContext();
        var customers = await db.Partners.AsNoTracking().Where(p => p.Type != PartnerType.Supplier && p.Active).Select(p => p.Id).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var products = await db.Products.AsNoTracking().Where(p => p.Active).Select(p => new { p.Id, p.SalePrice }).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var userId = await db.Users.AsNoTracking().OrderBy(u => u.Id).Select(u => (long?)u.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var categoryId = await db.FinancialCategories.AsNoTracking().Where(c => c.Kind == FinancialCategoryKind.Revenue && c.Name == "Vendas").Select(c => (long?)c.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        var lastNumber = await db.Sales.AsNoTracking().MaxAsync(s => (int?)s.Number, cancellationToken).ConfigureAwait(false) ?? 0;
        if (customers.Length == 0 || products.Length == 0 || userId is null || categoryId is null)
        {
            Console.WriteLine("Vendas: precisa de clientes, produtos ativos, um usuário e a categoria \"Vendas\". Pulando.");
            return;
        }

        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));

        var random = new Random(seed);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3));
        var start = today.AddDays(-730);
        var totals = (Sales: 0, Items: 0, Movements: 0, Entries: 0);

        for (var offset = 0; offset < count; offset += Batch)
        {
            var size = Math.Min(Batch, count - offset);
            var sales = new List<SeedSale>(size);
            for (var i = 0; i < size; i++)
            {
                var number = lastNumber + offset + i + 1;
                var roll = random.Next(100);
                var status = roll < 85 ? SaleStatus.Confirmed : roll < 90 ? SaleStatus.Cancelled : SaleStatus.Draft;
                // Datas crescem com o número (a sequência acompanha o tempo); rascunhos são da última semana.
                var issued = status == SaleStatus.Draft
                    ? today.AddDays(-random.Next(0, 7))
                    : start.AddDays((int)((long)(offset + i) * 723 / count));
                var method = Methods[random.Next(Methods.Length)];
                var installments = Sale.IsImmediate(method) ? 1 : random.Next(1, 7);
                var items = Enumerable.Range(0, random.Next(1, 5)).Select(_ =>
                {
                    var product = products[random.Next(products.Length)];
                    var quantity = (decimal)random.Next(1, 4);
                    return (product.Id, Quantity: quantity, Price: product.SalePrice, Total: Math.Round(quantity * product.SalePrice, 2));
                }).ToList();
                var subtotal = items.Sum(item => item.Total);
                var discount = random.Next(100) < 15 ? Math.Round(subtotal * 0.05m, 2) : 0m;
                sales.Add(new SeedSale(number, customers[random.Next(customers.Length)], status, method, installments, discount, subtotal, issued, items));
            }

            totals.Sales += await BulkInsert.ExecuteAsync(connection, "sale", SaleColumns, [.. sales.Select(s =>
            {
                var at = s.IssuedAt.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
                return new object?[]
                {
                    s.Number, s.PartnerId, s.Status.ToString(), s.Method.ToString(), s.Installments, s.Discount, s.Subtotal,
                    s.Subtotal - s.Discount, s.IssuedAt.ToDateTime(TimeOnly.MinValue), s.Status == SaleStatus.Confirmed ? at : null, at, at, 1,
                };
            })], cancellationToken).ConfigureAwait(false);

            var first = sales[0].Number;
            var last = sales[^1].Number;
            var ids = await db.Sales.AsNoTracking()
                .Where(s => s.Number >= first && s.Number <= last)
                .Select(s => new { s.Id, s.Number })
                .ToDictionaryAsync(s => s.Number, s => s.Id, cancellationToken)
                .ConfigureAwait(false);

            var itemRows = new List<object?[]>();
            var movementRows = new List<object?[]>();
            var entryRows = new List<object?[]>();
            foreach (var sale in sales)
            {
                var saleId = ids[sale.Number];
                var at = sale.IssuedAt.ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc);
                foreach (var item in sale.Items)
                {
                    itemRows.Add([saleId, item.Id, item.Quantity, item.Price, 0m, item.Total, at, at, 1]);
                    if (sale.Status == SaleStatus.Confirmed)
                    {
                        movementRows.Add([item.Id, nameof(StockMovementType.Sale), -item.Quantity, at, StockMovement.SaleReference, saleId, userId, $"Venda {sale.Number}", at, at, 1]);
                    }
                }

                if (sale.Status == SaleStatus.Confirmed)
                {
                    entryRows.AddRange(Receivables(random, sale, saleId, categoryId.Value, today, at));
                }
            }

            totals.Items += await BulkInsert.ExecuteAsync(connection, "sale_item", ItemColumns, itemRows, cancellationToken).ConfigureAwait(false);
            totals.Movements += await BulkInsert.ExecuteAsync(connection, "stock_movement", MovementColumns, movementRows, cancellationToken).ConfigureAwait(false);
            totals.Entries += await BulkInsert.ExecuteAsync(connection, "financial_entry", EntryColumns, entryRows, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"  {offset + size:N0}/{count:N0} vendas");
        }

        Console.WriteLine($"  {totals.Sales:N0} vendas, {totals.Items:N0} itens, {totals.Movements:N0} saídas de estoque e {totals.Entries:N0} parcelas em {stopwatch.Elapsed:mm\\:ss}");
        await FinishAsync(db, start, userId.Value, lastNumber + count, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Parcelas como a confirmação geraria: à vista, uma já recebida; a prazo, mensais.</summary>
    private static IEnumerable<object?[]> Receivables(Random random, SeedSale sale, long saleId, long categoryId, DateOnly today, DateTime at)
    {
        var total = sale.Subtotal - sale.Discount;
        var share = Math.Floor(total / sale.Installments * 100m) / 100m;
        var immediate = Sale.IsImmediate(sale.Method);
        for (var number = 1; number <= sale.Installments; number++)
        {
            var amount = number == 1 ? total - (share * (sale.Installments - 1)) : share;
            var due = immediate ? sale.IssuedAt : sale.IssuedAt.AddMonths(number);
            var paid = immediate || (due < today && random.Next(100) < 92);
            var paidAt = paid ? (immediate ? due : due.AddDays(random.Next(-2, 4))) : (DateOnly?)null;
            paidAt = paidAt > today ? today : paidAt;
            yield return
            [
                nameof(FinancialEntryKind.Receivable), categoryId, sale.PartnerId, saleId, $"Venda {sale.Number}", amount,
                due.ToDateTime(TimeOnly.MinValue), (paid ? FinancialEntryStatus.Paid : FinancialEntryStatus.Open).ToString(),
                paidAt?.ToDateTime(TimeOnly.MinValue), paid ? amount : null, number, sale.Installments, at, at, 1,
            ];
        }
    }

    /// <summary>
    /// Reposição para quem ficaria negativo, saldos recalculados e sequência de números ajustada. Todo SQL
    /// aqui é fixo, sem valor de fora (os parâmetros vão como parâmetro).
    /// </summary>
    private static async Task FinishAsync(ErpDbContext db, DateOnly start, long userId, int lastNumber, CancellationToken cancellationToken)
    {
        var replenishAt = start.AddDays(-1).ToDateTime(new TimeOnly(8, 0), DateTimeKind.Utc);
        var replenished = await db.Database.ExecuteSqlAsync(
            $"""
            INSERT INTO stock_movement (product_id, type, quantity, occurred_at, user_id, note, created_at, updated_at, version)
            SELECT s.product_id, 'Entry', 20 - s.total, {replenishAt}, {userId}, 'Reposição (seed)', {replenishAt}, {replenishAt}, 1
            FROM (SELECT product_id, SUM(quantity) AS total FROM stock_movement GROUP BY product_id) s
            WHERE s.total < 0
            """,
            cancellationToken).ConfigureAwait(false);
        Console.WriteLine($"  {replenished:N0} produtos receberam entrada de reposição; recalculando saldos...");

        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE stock_balance b
            JOIN (SELECT product_id, SUM(quantity) AS total FROM stock_movement GROUP BY product_id) s
              ON s.product_id = b.product_id
            SET b.quantity = s.total, b.updated_at = UTC_TIMESTAMP(6)
            """,
            cancellationToken).ConfigureAwait(false);
        await db.Database.ExecuteSqlAsync(
            $"UPDATE document_sequence SET last_number = GREATEST(last_number, {lastNumber}) WHERE name = 'sale'",
            cancellationToken).ConfigureAwait(false);
    }

    private sealed record SeedSale(
        int Number,
        long PartnerId,
        SaleStatus Status,
        PaymentMethod Method,
        int Installments,
        decimal Discount,
        decimal Subtotal,
        DateOnly IssuedAt,
        List<(long Id, decimal Quantity, decimal Price, decimal Total)> Items);
}
