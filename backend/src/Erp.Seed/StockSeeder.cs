using System.Data.Common;
using System.Diagnostics;
using Erp.Domain.Stock;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Seed;

/// <summary>
/// Movimentações sintéticas para a medição de performance (1 milhão no perfil full). Cada produto
/// recebe uma sequência no tempo que nunca deixa o saldo negativo: começa com uma entrada e mistura
/// saídas, entradas e ajustes nos últimos dois anos. Depois, o saldo de cada produto é recalculado no
/// banco como a soma das movimentações (a mesma invariante que a API mantém).
///
/// Por volume, grava com INSERT de várias linhas por comando, sempre parametrizado (nada de concatenar
/// valores). O EF levaria cerca de 20 minutos para 1 milhão de linhas.
/// </summary>
internal sealed class StockSeeder(Func<ErpDbContext> createContext, int movements, int seed)
{
    private const int RowsPerCommand = 1_000;
    private const int ColumnsPerRow = 6;
    private static readonly DateTime End = DateTime.UtcNow;
    private static readonly DateTime Start = End.AddDays(-730);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        await using var db = createContext();
        var productIds = await db.Products.AsNoTracking().OrderBy(p => p.Id).Select(p => p.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var userId = await db.Users.AsNoTracking().OrderBy(u => u.Id).Select(u => (long?)u.Id).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        if (productIds.Count == 0 || userId is null)
        {
            Console.WriteLine("Estoque: precisa de produtos e de ao menos um usuário (suba a API uma vez para criar o Admin). Pulando.");
            return;
        }

        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

        var random = new Random(seed);
        // Divide o total entre os produtos; os primeiros ficam com uma a mais até fechar a conta.
        var perProduct = movements / productIds.Count;
        var withExtra = movements % productIds.Count;
        var buffer = new List<Row>(RowsPerCommand);
        var written = 0;

        for (var index = 0; index < productIds.Count; index++)
        {
            var count = perProduct + (index < withExtra ? 1 : 0);
            foreach (var row in ProductHistory(random, productIds[index], Math.Max(1, count)))
            {
                buffer.Add(row);
                if (buffer.Count == RowsPerCommand)
                {
                    written += await InsertAsync(connection, buffer, userId.Value, cancellationToken).ConfigureAwait(false);
                    buffer.Clear();
                    if (written % 100_000 == 0)
                    {
                        Console.WriteLine($"  {written:N0}/{movements:N0} movimentações");
                    }
                }
            }
        }

        if (buffer.Count > 0)
        {
            written += await InsertAsync(connection, buffer, userId.Value, cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"  {written:N0} movimentações em {stopwatch.Elapsed:mm\\:ss}; recalculando saldos...");
        await RecalculateBalancesAsync(db, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Sequência de um produto em ordem de data, com saldo sempre maior ou igual a zero.</summary>
    private static IEnumerable<Row> ProductHistory(Random random, long productId, int count)
    {
        var step = (End - Start).Ticks / (count + 1);
        var balance = 0m;
        for (var i = 0; i < count; i++)
        {
            var at = new DateTime(Start.Ticks + (step * (i + 1)) + random.NextInt64(step / 2), DateTimeKind.Utc);
            var roll = random.Next(100);
            StockMovementType type;
            decimal quantity;
            if (i == 0 || balance < 5 || roll < 30)
            {
                type = StockMovementType.Entry;
                quantity = random.Next(10, 200);
            }
            else if (roll < 92)
            {
                type = StockMovementType.Exit;
                quantity = -Math.Min(balance, random.Next(1, 30));
            }
            else
            {
                type = StockMovementType.Adjustment;
                quantity = -Math.Min(balance, random.Next(1, 4));
            }

            balance += quantity;
            yield return new Row(productId, type, quantity, type == StockMovementType.Entry ? random.Next(100, 20_000) / 100m : null, at);
        }
    }

    private static async Task<int> InsertAsync(DbConnection connection, List<Row> rows, long userId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        var values = new System.Text.StringBuilder(
            "INSERT INTO stock_movement (product_id, type, quantity, unit_cost, occurred_at, created_at, user_id, updated_at, version, note) VALUES ");
        AddParameter(command, "@user", userId);
        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var p = i * ColumnsPerRow;
            values.Append(i == 0 ? "" : ",")
                .Append($"(@p{p},@p{p + 1},@p{p + 2},@p{p + 3},@p{p + 4},@p{p + 5},@user,@p{p + 5},1,")
                .Append(row.Type == StockMovementType.Adjustment ? "'Inventário periódico')" : "NULL)");
            AddParameter(command, $"@p{p}", row.ProductId);
            AddParameter(command, $"@p{p + 1}", row.Type.ToString());
            AddParameter(command, $"@p{p + 2}", row.Quantity);
            AddParameter(command, $"@p{p + 3}", row.UnitCost);
            AddParameter(command, $"@p{p + 4}", row.OccurredAt);
            AddParameter(command, $"@p{p + 5}", row.OccurredAt);
        }

#pragma warning disable CA2100 // Só nomes de parâmetro e constantes; todo valor vai por parâmetro.
        command.CommandText = values.ToString();
#pragma warning restore CA2100
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddParameter(DbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    /// <summary>Saldo = soma das movimentações, para todo produto (os que não têm linha de saldo ganham uma).</summary>
    private static async Task RecalculateBalancesAsync(ErpDbContext db, CancellationToken cancellationToken)
    {
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(10));
        await db.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO stock_balance (product_id, quantity, created_at, updated_at, version)
            SELECT p.id, 0, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), 1
            FROM product p
            WHERE NOT EXISTS (SELECT 1 FROM stock_balance b WHERE b.product_id = p.id)
            """,
            cancellationToken).ConfigureAwait(false);
        await db.Database.ExecuteSqlRawAsync(
            """
            UPDATE stock_balance b
            JOIN (SELECT product_id, SUM(quantity) AS total FROM stock_movement GROUP BY product_id) s
              ON s.product_id = b.product_id
            SET b.quantity = s.total, b.updated_at = UTC_TIMESTAMP(6)
            """,
            cancellationToken).ConfigureAwait(false);
    }

    private sealed record Row(long ProductId, StockMovementType Type, decimal Quantity, decimal? UnitCost, DateTime OccurredAt);
}
