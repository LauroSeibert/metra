using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Erp.Infrastructure.Persistence;

/// <summary>
/// Aplica as migrations pendentes antes de a API aceitar requisições. Só é registrado quando
/// <c>Database:MigrateOnStartup</c> é true (desenvolvimento e docker compose).
/// </summary>
internal sealed partial class DatabaseMigrator(IServiceScopeFactory scopeFactory, ILogger<DatabaseMigrator> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var scope = scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).ToList();
            if (pending.Count == 0)
            {
                return;
            }

            var names = string.Join(", ", pending);
            LogApplying(logger, pending.Count, names);
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(Level = LogLevel.Information, Message = "Aplicando {Count} migration(s): {Migrations}")]
    private static partial void LogApplying(ILogger logger, int count, string migrations);
}
