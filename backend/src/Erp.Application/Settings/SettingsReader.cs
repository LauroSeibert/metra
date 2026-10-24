using System.Globalization;
using Erp.Application.Abstractions;
using Erp.Domain.Settings;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Settings;

/// <summary>Lê parâmetros da empresa (app_setting). Valor ausente ou inválido vale o padrão da chave.</summary>
public sealed class SettingsReader(IErpDbContext db)
{
    public async Task<bool> GetBoolAsync(string key, bool fallback, CancellationToken cancellationToken)
    {
        var value = await ReadAsync(key, cancellationToken).ConfigureAwait(false);
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
    }

    public async Task<decimal> GetDecimalAsync(string key, decimal fallback, CancellationToken cancellationToken)
    {
        var value = await ReadAsync(key, cancellationToken).ConfigureAwait(false);
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : fallback;
    }

    public async Task<long?> GetLongAsync(string key, CancellationToken cancellationToken)
    {
        var value = await ReadAsync(key, cancellationToken).ConfigureAwait(false);
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }

    public Task<decimal> SalesMaxDiscountPercentAsync(CancellationToken cancellationToken) =>
        GetDecimalAsync(SettingKeys.SalesMaxDiscountPercent, SettingKeys.DefaultMaxDiscountPercent, cancellationToken);

    public Task<bool> StockAllowNegativeAsync(CancellationToken cancellationToken) =>
        GetBoolAsync(SettingKeys.StockAllowNegative, fallback: false, cancellationToken);

    private Task<string?> ReadAsync(string key, CancellationToken cancellationToken) =>
        db.AppSettings.AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => (string?)s.Value)
            .FirstOrDefaultAsync(cancellationToken);
}
