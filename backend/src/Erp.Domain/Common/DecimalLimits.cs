namespace Erp.Domain.Common;

/// <summary>Limites das colunas DECIMAL do banco (ver docs/plan.md, Modelo de dados).</summary>
public static class DecimalLimits
{
    /// <summary>Dinheiro: DECIMAL(18,2).</summary>
    public const int MoneyPrecision = 18;
    public const int MoneyScale = 2;
    public const decimal MoneyMax = 9_999_999_999_999_999.99m;

    /// <summary>Quantidade: DECIMAL(18,3).</summary>
    public const int QuantityPrecision = 18;
    public const int QuantityScale = 3;
    public const decimal QuantityMax = 999_999_999_999_999.999m;
}
