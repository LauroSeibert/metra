using System.Data.Common;
using System.Text;

namespace Erp.Seed;

/// <summary>
/// INSERT de várias linhas por comando, com todo valor em parâmetro (nunca concatenado). Para o volume do
/// seed: o EF levaria dezenas de minutos para milhões de linhas.
/// </summary>
internal static class BulkInsert
{
    /// <summary>Limite de parâmetros por comando, longe dos 65.535 do MySQL.</summary>
    private const int MaxParameters = 12_000;

    public static async Task<int> ExecuteAsync(
        DbConnection connection,
        string table,
        IReadOnlyList<string> columns,
        IReadOnlyList<object?[]> rows,
        CancellationToken cancellationToken)
    {
        var rowsPerCommand = Math.Max(1, MaxParameters / columns.Count);
        var written = 0;
        for (var start = 0; start < rows.Count; start += rowsPerCommand)
        {
            await using var command = connection.CreateCommand();
            var sql = new StringBuilder($"INSERT INTO {table} ({string.Join(", ", columns)}) VALUES ");
            var end = Math.Min(rows.Count, start + rowsPerCommand);
            for (var row = start; row < end; row++)
            {
                sql.Append(row == start ? "(" : ",(");
                for (var column = 0; column < columns.Count; column++)
                {
                    var name = $"@p{((row - start) * columns.Count) + column}";
                    sql.Append(column == 0 ? name : "," + name);
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = name;
                    parameter.Value = rows[row][column] ?? DBNull.Value;
                    command.Parameters.Add(parameter);
                }

                sql.Append(')');
            }

#pragma warning disable CA2100 // Tabela e colunas são constantes do seed; todo valor vai por parâmetro.
            command.CommandText = sql.ToString();
#pragma warning restore CA2100
            written += await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        return written;
    }
}
