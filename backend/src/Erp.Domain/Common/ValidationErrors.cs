namespace Erp.Domain.Common;

/// <summary>Acumula erros por campo e lança <see cref="DomainValidationException"/> no fim.</summary>
public sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public bool HasErrors => _errors.Count > 0;

    public ValidationErrors Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var messages))
        {
            messages = [];
            _errors[field] = messages;
        }

        messages.Add(message);
        return this;
    }

    public ValidationErrors Required(string field, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Add(field, "Campo obrigatório.");
        }

        return MaxLength(field, value, maxLength);
    }

    public ValidationErrors MaxLength(string field, string? value, int maxLength)
    {
        if (value is not null && value.Trim().Length > maxLength)
        {
            Add(field, $"Use no máximo {maxLength} caracteres.");
        }

        return this;
    }

    /// <summary>Valor não negativo, com no máximo <paramref name="scale"/> casas e dentro do limite da coluna DECIMAL.</summary>
    public ValidationErrors NonNegativeDecimal(string field, decimal value, int scale, decimal max)
    {
        if (value < 0)
        {
            Add(field, "O valor não pode ser negativo.");
        }
        else if (value > max)
        {
            Add(field, $"O valor máximo é {max}.");
        }

        if (decimal.Round(value, scale) != value)
        {
            Add(field, $"Use no máximo {scale} casas decimais.");
        }

        return this;
    }

    public ValidationErrors PositiveId(string field, long value)
    {
        if (value <= 0)
        {
            Add(field, "Campo obrigatório.");
        }

        return this;
    }

    /// <summary>Referência opcional: nula é válida; informada, precisa ser um id positivo.</summary>
    public ValidationErrors OptionalPositiveId(string field, long? value)
    {
        if (value is <= 0)
        {
            Add(field, "Identificador inválido.");
        }

        return this;
    }

    public void ThrowIfAny()
    {
        if (HasErrors)
        {
            throw new DomainValidationException(_errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()));
        }
    }
}
