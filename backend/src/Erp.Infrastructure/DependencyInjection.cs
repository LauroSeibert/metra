using Erp.Application.Abstractions;
using Erp.Infrastructure.HealthChecks;
using Erp.Infrastructure.Identity;
using Erp.Infrastructure.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
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
        services.AddErpIdentity(configuration);

        if (configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            services.AddHostedService<DatabaseMigrator>();
        }

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: [ReadyTag]);

        return services;
    }

    /// <summary>
    /// Identity (senha, bloqueio, perfis) e tokens bearer do ASP.NET Core. Decisões no ADR 0003.
    /// </summary>
    private static void AddErpIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddIdentityCore<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<AppRole>()
            .AddEntityFrameworkStores<ErpDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        services.AddAuthentication(IdentityConstants.BearerScheme)
            .AddBearerToken(IdentityConstants.BearerScheme, options =>
            {
                options.BearerTokenExpiration = TimeSpan.FromMinutes(15);
                options.RefreshTokenExpiration = TimeSpan.FromDays(7);
            });

        // Chaves que assinam os tokens: num volume, para a sessão sobreviver a um restart do container.
        var keysPath = configuration["DataProtection:KeysPath"];
        var dataProtection = services.AddDataProtection().SetApplicationName("erp-demo");
        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }

        services.AddScoped<IUserAccounts, UserAccounts>();
        services.AddScoped<ISessionTokens, SessionTokens>();
        services.AddScoped<IdentitySeeder>();
    }
}
