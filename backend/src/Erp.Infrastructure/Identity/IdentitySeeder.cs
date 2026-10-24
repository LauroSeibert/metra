using Erp.Domain.Access;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Erp.Infrastructure.Identity;

/// <summary>
/// Garante os três perfis e o primeiro Admin. O Admin inicial vem de configuração
/// (<c>Auth:BootstrapAdmin:Email</c> e <c>:Password</c>, no .env do compose) e só é criado se não
/// houver nenhum usuário. Em desenvolvimento, <c>Auth:DemoUsersPassword</c> cria também um
/// operador e um usuário de leitura para testar os perfis.
/// </summary>
internal sealed partial class IdentitySeeder(
    RoleManager<AppRole> roles,
    UserManager<AppUser> users,
    IConfiguration configuration,
    ILogger<IdentitySeeder> logger)
{
    public const string DemoOperatorEmail = "operador@erp.local";
    public const string DemoViewerEmail = "leitura@erp.local";

    public async Task SeedAsync()
    {
        foreach (var role in Roles.All)
        {
            if (!await roles.RoleExistsAsync(role).ConfigureAwait(false))
            {
                await roles.CreateAsync(new AppRole(role)).ConfigureAwait(false);
            }
        }

        var adminEmail = configuration["Auth:BootstrapAdmin:Email"];
        var adminPassword = configuration["Auth:BootstrapAdmin:Password"];
        if (!users.Users.Any() && !string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
        {
            await CreateAsync(adminEmail, "Administrador", Roles.Admin, adminPassword).ConfigureAwait(false);
        }

        var demoPassword = configuration["Auth:DemoUsersPassword"];
        if (!string.IsNullOrWhiteSpace(demoPassword))
        {
            await CreateAsync(DemoOperatorEmail, "Operador demo", Roles.Operator, demoPassword).ConfigureAwait(false);
            await CreateAsync(DemoViewerEmail, "Leitura demo", Roles.Viewer, demoPassword).ConfigureAwait(false);
        }
    }

    private async Task CreateAsync(string email, string fullName, string role, string password)
    {
        if (await users.FindByEmailAsync(email).ConfigureAwait(false) is not null)
        {
            return;
        }

        var user = new AppUser { UserName = email, Email = email, EmailConfirmed = true, FullName = fullName };
        var result = await users.CreateAsync(user, password).ConfigureAwait(false);
        if (result.Succeeded)
        {
            await users.AddToRoleAsync(user, role).ConfigureAwait(false);
            LogCreated(logger, email, role);
        }
        else
        {
            LogFailed(logger, email, string.Join("; ", result.Errors.Select(e => e.Code)));
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Usuário inicial {Email} criado com o perfil {Role}")]
    private static partial void LogCreated(ILogger logger, string email, string role);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Não foi possível criar o usuário inicial {Email}: {Errors}")]
    private static partial void LogFailed(ILogger logger, string email, string errors);
}
