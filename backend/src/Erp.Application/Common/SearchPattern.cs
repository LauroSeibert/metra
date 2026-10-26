namespace Erp.Application.Common;

/// <summary>
/// Padrões para <c>EF.Functions.Like</c>. Busca por prefixo (<c>termo%</c>) para o MySQL usar o índice;
/// curingas digitados pelo usuário são escapados com <see cref="EscapeCharacter"/>.
/// O escape é "!" e não a barra invertida: o provider MySql.EntityFrameworkCore gera <c>ESCAPE '\'</c> sem
/// escapar a barra, o que quebra o SQL no MySQL (ver docs/modules/products.md).
/// </summary>
public static class SearchPattern
{
    public const string EscapeCharacter = "!";

    /// <summary>Devolve null quando o termo é vazio (sem filtro).</summary>
    public static string? Prefix(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return null;
        }

        return Escape(term) + "%";
    }

    /// <summary>
    /// Busca em qualquer parte do texto (<c>%termo%</c>). Não usa índice: varre a tabela, o que só vale onde a
    /// medição aceita (produtos, ver docs/modules/products.md). Null quando o termo é vazio.
    /// </summary>
    public static string? Contains(string? term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return null;
        }

        return "%" + Escape(term) + "%";
    }

    private static string Escape(string term) =>
        term.Trim()
            .Replace(EscapeCharacter, EscapeCharacter + EscapeCharacter, StringComparison.Ordinal)
            .Replace("%", EscapeCharacter + "%", StringComparison.Ordinal)
            .Replace("_", EscapeCharacter + "_", StringComparison.Ordinal);
}
