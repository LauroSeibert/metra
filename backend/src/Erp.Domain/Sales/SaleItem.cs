using System.Globalization;
using Erp.Domain.Common;

namespace Erp.Domain.Sales;

/// <summary>
/// Item da venda. O preço unitário é copiado do produto quando o item entra no rascunho e não muda
/// depois, mesmo que o produto mude de preço.
/// </summary>
public sealed class SaleItem : Entity
{
    private SaleItem()
    {
    }

    public long SaleId { get; private set; }

    public long ProductId { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    /// <summary>Desconto do item (valor).</summary>
    public decimal Discount { get; private set; }

    /// <summary>Quantidade × preço − desconto.</summary>
    public decimal Total { get; private set; }

    /// <summary>Cria o item; erros vão para <paramref name="errors"/> com a chave <c>items</c>.</summary>
    internal static SaleItem Create(SaleItemData data, int index, ValidationErrors errors)
    {
        var position = (index + 1).ToString(CultureInfo.InvariantCulture);
        if (data.ProductId <= 0)
        {
            errors.Add("items", $"Item {position}: escolha um produto.");
        }

        var itemErrors = new ValidationErrors()
            .NonNegativeDecimal("quantity", data.Quantity, DecimalLimits.QuantityScale, DecimalLimits.QuantityMax)
            .NonNegativeDecimal("unitPrice", data.UnitPrice, DecimalLimits.MoneyScale, DecimalLimits.MoneyMax)
            .NonNegativeDecimal("discount", data.Discount, DecimalLimits.MoneyScale, DecimalLimits.MoneyMax);
        if (itemErrors.HasErrors || data.Quantity == 0)
        {
            errors.Add("items", $"Item {position}: quantidade maior que zero (até 3 casas) e valores não negativos.");
        }

        var gross = Math.Round(data.Quantity * data.UnitPrice, 2, MidpointRounding.AwayFromZero);
        if (data.Discount > gross)
        {
            errors.Add("items", $"Item {position}: o desconto é maior que o valor do item.");
        }

        return new SaleItem
        {
            ProductId = data.ProductId,
            Quantity = data.Quantity,
            UnitPrice = data.UnitPrice,
            Discount = data.Discount,
            Total = gross - data.Discount,
        };
    }
}

/// <summary>Item a gravar. <see cref="UnitPrice"/> vem do produto (o caso de uso copia), não do cliente.</summary>
public sealed record SaleItemData(long ProductId, decimal Quantity, decimal UnitPrice, decimal Discount);
