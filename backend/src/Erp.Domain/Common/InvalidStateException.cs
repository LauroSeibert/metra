namespace Erp.Domain.Common;

/// <summary>
/// Ação que o estado atual do registro não permite (editar lançamento pago, confirmar venda já
/// confirmada). Vira 409 com o código <c>invalid_state</c>.
/// </summary>
public sealed class InvalidStateException : Exception
{
    public InvalidStateException()
        : base("A ação não é permitida no estado atual do registro.")
    {
    }

    public InvalidStateException(string message)
        : base(message)
    {
    }

    public InvalidStateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
