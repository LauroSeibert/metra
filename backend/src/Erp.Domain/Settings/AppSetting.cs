using Erp.Domain.Common;

namespace Erp.Domain.Settings;

/// <summary>
/// Parâmetro da empresa (tabela <c>app_setting</c>): chave única e valor em texto, validado na
/// leitura por <see cref="SettingKeys"/>. A tela de configurações chega na Fase 8.
/// </summary>
public sealed class AppSetting : Entity
{
    public const int KeyMaxLength = 100;
    public const int ValueMaxLength = 500;

    private AppSetting()
    {
    }

    public string Key { get; private set; } = string.Empty;

    public string Value { get; private set; } = string.Empty;

    public static AppSetting Create(string key, string value)
    {
        var setting = new AppSetting { Key = key };
        setting.Change(value);
        return setting;
    }

    public void Change(string value)
    {
        new ValidationErrors().MaxLength("value", value, ValueMaxLength).ThrowIfAny();
        Value = value ?? string.Empty;
    }
}

/// <summary>Chaves conhecidas e seus padrões. Chave ausente ou valor inválido vale o padrão.</summary>
public static class SettingKeys
{
    /// <summary>Permite saldo de estoque negativo (padrão: não).</summary>
    public const string StockAllowNegative = "stock.allowNegative";

    /// <summary>Desconto máximo de uma venda, em % do valor bruto (padrão: 10).</summary>
    public const string SalesMaxDiscountPercent = "sales.maxDiscountPercent";

    /// <summary>Categoria de receita das parcelas geradas por vendas (padrão: a categoria "Vendas").</summary>
    public const string SalesRevenueCategoryId = "sales.revenueCategoryId";

    public const decimal DefaultMaxDiscountPercent = 10m;
}
