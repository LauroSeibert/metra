using Erp.Application.Abstractions;
using Erp.Domain.Settings;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Settings;

/// <summary>Lê parâmetros da empresa (app_setting). Valor ausente ou inválido vale o padrão da chave.</summary>
public sealed class SettingsReader(IErpDbContext db)
{
    public async Task<bool> GetBoolAsync(string key, bool fallback, CancellationToken cancellationToken)
    {
        var value = await db.AppSettings.AsNoTracking()
            .Where(s => s.Key == key)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return bool.TryParse(value, out var parsed) ? parsed : fallback;
    }

    public Task<bool> StockAllowNegativeAsync(CancellationToken cancellationToken) =>
        GetBoolAsync(SettingKeys.StockAllowNegative, fallback: false, cancellationToken);
}
