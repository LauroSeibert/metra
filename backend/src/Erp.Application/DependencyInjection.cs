using Erp.Application.Products;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ProductService>();
        services.AddScoped<ProductCategoryService>();
        services.AddScoped<UnitService>();
        return services;
    }
}
