namespace Erp.Domain.Common;

/// <summary>
/// Numeração sequencial sem lacunas (tabela <c>document_sequence</c>), uma linha por tipo de documento.
/// Atualizada na mesma transação do documento, com concorrência otimista pela versão: duas vendas ao
/// mesmo tempo não recebem o mesmo número; a segunda refaz a operação.
/// </summary>
public sealed class DocumentSequence : Entity
{
    public const int NameMaxLength = 30;

    /// <summary>Sequência das vendas.</summary>
    public const string Sale = "sale";

    private DocumentSequence()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public long LastNumber { get; private set; }

    public static DocumentSequence Create(string name) => new() { Name = name };

    public long Next() => ++LastNumber;
}
