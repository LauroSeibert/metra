using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Erp.Infrastructure.Persistence;

/// <summary>
/// Usado só pelo <c>dotnet ef</c> para gerar migrations. A string de conexão não precisa ser válida
/// para <c>migrations add</c>; para <c>database update</c>, defina ERP_DESIGN_CONNECTION.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ErpDbContext>
{
    public ErpDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ERP_DESIGN_CONNECTION")
            ?? "Server=localhost;Port=3306;Database=erp;User=erp;Password=design-time-only";
        var options = new DbContextOptionsBuilder<ErpDbContext>().UseMySQL(connectionString).Options;
        return new ErpDbContext(options, TimeProvider.System);
    }
}
