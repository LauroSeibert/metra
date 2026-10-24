namespace Erp.Domain.Common;

/// <summary>
/// Violação de regra de entrada de uma entidade. As chaves de <see cref="Errors"/> são os nomes
/// dos campos em camelCase, iguais aos do contrato da API, para o front marcar o campo certo.
/// </summary>
public sealed class DomainValidationException : Exception
{
    public DomainValidationException()
        : this(new Dictionary<string, string[]>())
    {
    }

    public DomainValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public DomainValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]>();
    }

    public DomainValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("Um ou mais campos são inválidos.")
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public static DomainValidationException For(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
