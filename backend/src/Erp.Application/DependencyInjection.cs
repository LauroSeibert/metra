using Erp.Application.Partners;
using Erp.Application.Products;
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
        services.AddScoped<StockService>();
        return services;
    }
}
