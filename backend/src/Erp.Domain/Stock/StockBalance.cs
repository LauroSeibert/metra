using System.Globalization;
using Erp.Domain.Common;

namespace Erp.Domain.Stock;

/// <summary>
/// Saldo de estoque de um produto (um único depósito). Atualizado na mesma transação de cada
/// movimentação, com concorrência otimista pela versão; nunca editado direto.
/// </summary>
public sealed class StockBalance : Entity
{
    private StockBalance()
    {
    }

    public long ProductId { get; private set; }

    public decimal Quantity { get; private set; }

    public static StockBalance Empty(long productId) => new() { ProductId = productId, Quantity = 0 };

    /// <summary>
    /// Soma a quantidade da movimentação. Sem permissão de saldo negativo (configuração
    /// <c>stock.allowNegative</c>), recusa o que deixaria o saldo abaixo de zero.
    /// </summary>
    public void Apply(StockMovement movement, bool allowNegative)
    {
        ArgumentNullException.ThrowIfNull(movement);
        if (movement.ProductId != ProductId)
        {
            throw new InvalidOperationException("Movimentação de outro produto.");
        }

        var result = Quantity + movement.Quantity;
        if (!allowNegative && result < 0)
        {
            // Vírgula decimal sem depender de cultura (o backend roda com InvariantGlobalization).
            var available = Quantity.ToString("0.###", CultureInfo.InvariantCulture).Replace('.', ',');
            new ValidationErrors()
                .Add("quantity", $"Saldo insuficiente: disponível {available}.")
                .ThrowIfAny();
        }

        Quantity = result;
    }
}
