using Erp.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Erp.Infrastructure.HealthChecks;

internal sealed class DatabaseHealthCheck(ErpDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Banco de dados indisponível.");
    }
}
