using Erp.Application.Dashboard;
using Erp.Application.Common;
using Erp.Application.Finance;
using Erp.Application.Partners;
using Erp.Application.Products;
using Erp.Application.Sales;
using Erp.Application.Settings;
using Erp.Application.Stock;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ProductService>();
        services.AddScoped<ProductCategoryService>();
        services.AddScoped<UnitService>();
        services.AddScoped<PartnerService>();
        services.AddScoped<SettingsReader>();
        services.AddScoped<SettingsService>();
        services.AddScoped<StockService>();
        services.AddScoped<BusinessCalendar>();
        services.AddScoped<FinancialCategoryService>();
        services.AddScoped<FinancialEntryService>();
        services.AddScoped<SaleService>();
        services.AddScoped<DashboardService>();
        return services;
    }
}
