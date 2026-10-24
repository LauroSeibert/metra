using Erp.Domain.Common;

namespace Erp.Domain.Stock;

/// <summary>Natureza da movimentação. A quantidade gravada tem sinal: entra positiva, sai negativa.</summary>
public enum StockMovementType
{
    /// <summary>Entrada manual (compra, devolução de cliente).</summary>
    Entry = 1,

    /// <summary>Saída manual (perda, consumo interno).</summary>
    Exit = 2,

    /// <summary>Ajuste de inventário: a diferença entre a contagem e o saldo.</summary>
    Adjustment = 3,

    /// <summary>Estorno de outra movimentação: quantidade oposta, referência ao movimento original.</summary>
    Reversal = 4,
}

/// <summary>
/// Movimentação de estoque. Imutável: só é inserida, nunca alterada nem excluída; correção é um novo
/// movimento de estorno. A soma das quantidades de um produto é igual ao saldo dele (StockBalance).
/// </summary>
public sealed class StockMovement : Entity
{
    public const int NoteMaxLength = 500;
    public const int ReferenceTypeMaxLength = 30;

    /// <summary>Tipo de referência de um estorno: o movimento original.</summary>
    public const string MovementReference = "StockMovement";

    private StockMovement()
    {
    }

    public long ProductId { get; private set; }

    public StockMovementType Type { get; private set; }

    /// <summary>Quantidade com sinal: positiva aumenta o saldo, negativa diminui.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Custo unitário informado na entrada (opcional nos outros tipos).</summary>
    public decimal? UnitCost { get; private set; }

    /// <summary>Quando a movimentação aconteceu (UTC); pode ser anterior ao registro.</summary>
    public DateTime OccurredAt { get; private set; }

    /// <summary>Origem do movimento (ex.: "StockMovement" num estorno; "Sale" a partir da Fase 6).</summary>
    public string? ReferenceType { get; private set; }

    public long? ReferenceId { get; private set; }

    /// <summary>Usuário que registrou.</summary>
    public long UserId { get; private set; }

    public string? Note { get; private set; }

    /// <summary>Entrada: <paramref name="quantity"/> positiva aumenta o saldo.</summary>
    public static StockMovement Entry(long productId, decimal quantity, decimal? unitCost, DateTime occurredAt, long userId, string? note) =>
        Create(productId, StockMovementType.Entry, Positive(quantity), quantity, unitCost, occurredAt, userId, note);

    /// <summary>Saída: <paramref name="quantity"/> positiva, gravada negativa.</summary>
    public static StockMovement Exit(long productId, decimal quantity, decimal? unitCost, DateTime occurredAt, long userId, string? note) =>
        Create(productId, StockMovementType.Exit, Positive(quantity), -quantity, unitCost, occurredAt, userId, note);

    /// <summary>
    /// Ajuste de inventário: o saldo passa a ser <paramref name="countedQuantity"/>. Grava a diferença
    /// em relação a <paramref name="currentBalance"/>; contagem igual ao saldo não gera movimento.
    /// </summary>
    public static StockMovement Adjustment(
        long productId,
        decimal countedQuantity,
        decimal currentBalance,
        DateTime occurredAt,
        long userId,
        string? note)
    {
        var errors = new ValidationErrors()
            .NonNegativeDecimal("quantity", countedQuantity, DecimalLimits.QuantityScale, DecimalLimits.QuantityMax);
        if (!errors.HasErrors && countedQuantity == currentBalance)
        {
            errors.Add("quantity", "A contagem é igual ao saldo atual; não há o que ajustar.");
        }

        if (string.IsNullOrWhiteSpace(note))
        {
            errors.Add("note", "Informe o motivo do ajuste.");
        }

        return Create(productId, StockMovementType.Adjustment, errors, countedQuantity - currentBalance, null, occurredAt, userId, note);
    }

    /// <summary>Estorno: quantidade oposta à do original, que não pode ser outro estorno.</summary>
    public static StockMovement Reverse(StockMovement original, DateTime occurredAt, long userId, string? note)
    {
        ArgumentNullException.ThrowIfNull(original);
        if (original.Type == StockMovementType.Reversal)
        {
            new ValidationErrors().Add("id", "Um estorno não pode ser estornado; registre um novo movimento.").ThrowIfAny();
        }

        var movement = Create(
            original.ProductId,
            StockMovementType.Reversal,
            new ValidationErrors(),
            -original.Quantity,
            original.UnitCost,
            occurredAt,
            userId,
            note);
        movement.ReferenceType = MovementReference;
        movement.ReferenceId = original.Id;
        return movement;
    }

    private static ValidationErrors Positive(decimal quantity)
    {
        var errors = new ValidationErrors()
            .NonNegativeDecimal("quantity", quantity, DecimalLimits.QuantityScale, DecimalLimits.QuantityMax);
        if (!errors.HasErrors && quantity == 0)
        {
            errors.Add("quantity", "Informe uma quantidade maior que zero.");
        }

        return errors;
    }

    private static StockMovement Create(
        long productId,
        StockMovementType type,
        ValidationErrors errors,
        decimal signedQuantity,
        decimal? unitCost,
        DateTime occurredAt,
        long userId,
        string? note)
    {
        errors.PositiveId("productId", productId);
        if (unitCost is { } cost)
        {
            errors.NonNegativeDecimal("unitCost", cost, DecimalLimits.MoneyScale, DecimalLimits.MoneyMax);
        }

        var trimmedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        errors.MaxLength("note", trimmedNote, NoteMaxLength);
        errors.ThrowIfAny();

        return new StockMovement
        {
            ProductId = productId,
            Type = type,
            Quantity = signedQuantity,
            UnitCost = unitCost,
            OccurredAt = occurredAt,
            UserId = userId,
            Note = trimmedNote,
        };
    }
}
