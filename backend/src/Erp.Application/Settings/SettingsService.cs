using Erp.Application.Abstractions;
using Erp.Domain.Common;
using Erp.Domain.Settings;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Settings;

/// <summary>Parâmetros da empresa exibidos e editados na tela de configurações.</summary>
public sealed record CompanySettingsDto(
    string CompanyName,
    string? CompanyDocument,
    decimal MaxDiscountPercent,
    bool AllowNegativeStock);

/// <summary>Leitura e gravação dos parâmetros da empresa (tabela app_setting, uma linha por chave).</summary>
public sealed class SettingsService(IErpDbContext db, SettingsReader reader)
{
    public const int CompanyNameMaxLength = 200;
    public const int CompanyDocumentMaxLength = 20;
    private const string DefaultCompanyName = "Metra Comércio";

    public async Task<CompanySettingsDto> GetAsync(CancellationToken cancellationToken)
    {
        var values = await db.AppSettings.AsNoTracking()
            .Where(s => s.Key == SettingKeys.CompanyName || s.Key == SettingKeys.CompanyDocument)
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);
        return new CompanySettingsDto(
            values.GetValueOrDefault(SettingKeys.CompanyName, DefaultCompanyName),
            values.GetValueOrDefault(SettingKeys.CompanyDocument),
            await reader.SalesMaxDiscountPercentAsync(cancellationToken),
            await reader.StockAllowNegativeAsync(cancellationToken));
    }

    public async Task<CompanySettingsDto> UpdateAsync(CompanySettingsDto request, CancellationToken cancellationToken)
    {
        var name = request.CompanyName?.Trim() ?? string.Empty;
        var document = string.IsNullOrWhiteSpace(request.CompanyDocument) ? null : request.CompanyDocument.Trim();
        var errors = new ValidationErrors()
            .Required("companyName", name, CompanyNameMaxLength)
            .MaxLength("companyDocument", document, CompanyDocumentMaxLength);
        if (request.MaxDiscountPercent is < 0 or > 100)
        {
            errors.Add("maxDiscountPercent", "Use de 0 a 100%.");
        }
        errors.ThrowIfAny();

        await SetAsync(SettingKeys.CompanyName, name, cancellationToken);
        await SetAsync(SettingKeys.CompanyDocument, document ?? string.Empty, cancellationToken);
        await SetAsync(
            SettingKeys.SalesMaxDiscountPercent,
            request.MaxDiscountPercent.ToString(System.Globalization.CultureInfo.InvariantCulture),
            cancellationToken);
        await SetAsync(SettingKeys.StockAllowNegative, request.AllowNegativeStock ? "true" : "false", cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return await GetAsync(cancellationToken);
    }

    private async Task SetAsync(string key, string value, CancellationToken cancellationToken)
    {
        var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (setting is null)
        {
            db.AppSettings.Add(AppSetting.Create(key, value));
        }
        else
        {
            setting.Change(value);
        }
    }
}
