using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace Erp.Infrastructure.Persistence;

/// <summary>Traduz erros do MySQL para algo que a API entende sem conhecer o provider.</summary>
public static class DbExceptionClassifier
{
    private const int DuplicateEntry = 1062;

    /// <summary>Violação de índice único que escapou da checagem prévia (corrida entre duas gravações).</summary>
    public static bool IsUniqueViolation(Exception exception) =>
        exception is DbUpdateException { InnerException: MySqlException { Number: DuplicateEntry } };
}
