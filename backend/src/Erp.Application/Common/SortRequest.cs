using Erp.Domain.Common;

namespace Erp.Application.Common;

/// <summary>
/// Ordenação recebida como <c>campo</c> ou <c>-campo</c> (decrescente). Só campos de uma lista fechada
/// são aceitos, para nunca ordenar por coluna sem índice nem montar SQL a partir da entrada.
/// </summary>
public sealed record SortRequest(string Field, bool Descending)
{
    public static SortRequest Parse(string? sort, string defaultField, IReadOnlyCollection<string> allowedFields)
    {
        ArgumentNullException.ThrowIfNull(allowedFields);
        if (string.IsNullOrWhiteSpace(sort))
        {
            return new SortRequest(defaultField, false);
        }

        var trimmed = sort.Trim();
        var descending = trimmed.StartsWith('-');
        var field = descending ? trimmed[1..] : trimmed;
        var match = allowedFields.FirstOrDefault(allowed => string.Equals(allowed, field, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            throw DomainValidationException.For("sort", $"Ordenação inválida. Use: {string.Join(", ", allowedFields)}.");
        }

        return new SortRequest(match, descending);
    }
}
