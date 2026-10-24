using System.Data.Common;
using System.Diagnostics;
using Erp.Domain.Finance;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Seed;

/// <summary>
/// Contas a receber e a pagar sintéticas (250 mil no perfil full; as vendas da Fase 6 somam as parcelas
/// delas). Vencimentos nos últimos dois anos e nos próximos seis meses: o que já venceu está quase todo
/// pago, uma parte segue em aberto (vencida) e poucas foram canceladas. Contas de 1 a 6 parcelas.
/// Mesmo INSERT parametrizado em lote do <see cref="StockSeeder"/>.
/// </summary>
internal sealed class FinanceSeeder(Func<ErpDbContext> createContext, int count, int seed)
{
    private const int RowsPerCommand = 1_000;
    private const int ColumnsPerRow = 12;

    private static readonly string[] PayableTexts = ["Compra de mercadoria", "Aluguel", "Energia", "Internet", "Folha de pagamento", "Frete", "Manutenção", "Imposto"];
    private static readonly string[] ReceivableTexts = ["Venda a prazo", "Serviço prestado", "Contrato mensal", "Locação de equipamento"];

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        await using var db = createContext();
        var categories = await db.FinancialCategories.AsNoTracking().Select(c => new { c.Id, c.Kind }).ToListAsync(cancellationToken).ConfigureAwait(false);
        var revenue = categories.Where(c => c.Kind == FinancialCategoryKind.Revenue).Select(c => c.Id).ToArray();
        var expense = categories.Where(c => c.Kind == FinancialCategoryKind.Expense).Select(c => c.Id).ToArray();
        var customers = await db.Partners.AsNoTracking().Where(p => p.Type != Domain.Partners.PartnerType.Supplier).Select(p => p.Id).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var suppliers = await db.Partners.AsNoTracking().Where(p => p.Type != Domain.Partners.PartnerType.Customer).Select(p => p.Id).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        if (revenue.Length == 0 || expense.Length == 0 || customers.Length == 0 || suppliers.Length == 0)
        {
            Console.WriteLine("Financeiro: precisa das categorias (migration) e de parceiros. Pulando.");
            return;
        }

        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var random = new Random(seed);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = today.AddDays(-730);
        var buffer = new List<Row>(RowsPerCommand);
        var written = 0;

        while (written + buffer.Count < count)
        {
            var payable = random.Next(100) < 60;
            var installments = random.Next(100) < 70 ? 1 : random.Next(2, 7);
            var total = Math.Round((decimal)(random.NextDouble() * (payable ? 8000 : 5000)) + 50m, 2);
            var share = Math.Floor(total / installments * 100m) / 100m;
            var firstDue = start.AddDays(random.Next(0, 730 + 180));
            var texts = payable ? PayableTexts : ReceivableTexts;
            var description = texts[random.Next(texts.Length)];
            var category = payable ? expense[random.Next(expense.Length)] : revenue[random.Next(revenue.Length)];
            var partner = payable ? suppliers[random.Next(suppliers.Length)] : customers[random.Next(customers.Length)];

            for (var number = 1; number <= installments && written + buffer.Count < count; number++)
            {
                var due = firstDue.AddMonths(number - 1);
                var amount = number == 1 ? total - (share * (installments - 1)) : share;
                var (status, paidAt, paidAmount) = Settle(random, due, today, amount);
                buffer.Add(new Row(
                    payable ? FinancialEntryKind.Payable : FinancialEntryKind.Receivable,
                    category,
                    partner,
                    description,
                    amount,
                    due,
                    status,
                    paidAt,
                    paidAmount,
                    number,
                    installments,
                    due.AddDays(-random.Next(1, 40)).ToDateTime(new TimeOnly(12, 0), DateTimeKind.Utc)));

                if (buffer.Count == RowsPerCommand)
                {
                    written += await InsertAsync(connection, buffer, cancellationToken).ConfigureAwait(false);
                    buffer.Clear();
                    if (written % 50_000 == 0)
                    {
                        Console.WriteLine($"  {written:N0}/{count:N0} lançamentos");
                    }
                }
            }
        }

        if (buffer.Count > 0)
        {
            written += await InsertAsync(connection, buffer, cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"  {written:N0} lançamentos em {stopwatch.Elapsed:mm\\:ss}");
    }

    /// <summary>Vencido: 85% pago (até 5 dias depois, às vezes com juros), 3% cancelado, o resto em aberto.</summary>
    private static (FinancialEntryStatus Status, DateOnly? PaidAt, decimal? PaidAmount) Settle(Random random, DateOnly due, DateOnly today, decimal amount)
    {
        var roll = random.Next(100);
        if (due < today && roll < 85)
        {
            var paidAt = due.AddDays(random.Next(-3, 6));
            paidAt = paidAt > today ? today : paidAt;
            var interest = paidAt > due ? Math.Round(amount * 0.01m, 2) : 0m;
            return (FinancialEntryStatus.Paid, paidAt, amount + interest);
        }

        return roll >= 97 ? (FinancialEntryStatus.Cancelled, null, null) : (FinancialEntryStatus.Open, null, null);
    }

    private static async Task<int> InsertAsync(DbConnection connection, List<Row> rows, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        var sql = new System.Text.StringBuilder(
            "INSERT INTO financial_entry (kind, category_id, partner_id, description, amount, due_date, status, paid_at, paid_amount, installment_no, installment_total, created_at, updated_at, version) VALUES ");
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var p = i * ColumnsPerRow;
            sql.Append(i == 0 ? "" : ",").Append('(');
            for (var column = 0; column < ColumnsPerRow; column++)
            {
                sql.Append(column == 0 ? "" : ",").Append($"@p{p + column}");
            }

            sql.Append($",@p{p + 11},1)");
            object?[] values =
            [
                row.Kind.ToString(), row.CategoryId, row.PartnerId, row.Description, row.Amount,
                row.DueDate.ToDateTime(TimeOnly.MinValue), row.Status.ToString(), row.PaidAt?.ToDateTime(TimeOnly.MinValue),
                row.PaidAmount, row.InstallmentNo, row.InstallmentTotal, row.CreatedAt,
            ];
            for (var column = 0; column < values.Length; column++)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = $"@p{p + column}";
                parameter.Value = values[column] ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }

#pragma warning disable CA2100 // Só nomes de parâmetro e constantes; todo valor vai por parâmetro.
        command.CommandText = sql.ToString();
#pragma warning restore CA2100
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private sealed record Row(
        FinancialEntryKind Kind,
        long CategoryId,
        long PartnerId,
        string Description,
        decimal Amount,
        DateOnly DueDate,
        FinancialEntryStatus Status,
        DateOnly? PaidAt,
        decimal? PaidAmount,
        int InstallmentNo,
        int InstallmentTotal,
        DateTime CreatedAt);
}
