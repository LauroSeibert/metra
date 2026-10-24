using Erp.Application.Abstractions;
using Erp.Infrastructure.HealthChecks;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Erp.Infrastructure;

public static class DependencyInjection
{
    public const string ReadyTag = "ready";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Erp")
            ?? throw new InvalidOperationException("Connection string 'Erp' não configurada.");

        services.AddDbContext<ErpDbContext>(options => options.UseMySQL(connectionString));
        services.AddScoped<IErpDbContext>(provider => provider.GetRequiredService<ErpDbContext>());

        if (configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            services.AddHostedService<DatabaseMigrator>();
        }

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: [ReadyTag]);

        return services;
    }
}
