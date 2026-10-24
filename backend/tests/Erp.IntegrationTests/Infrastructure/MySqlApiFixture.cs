using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MySql;

[assembly: AssemblyFixture(typeof(Erp.IntegrationTests.Infrastructure.MySqlApiFixture))]

namespace Erp.IntegrationTests.Infrastructure;

/// <summary>
/// Um MySQL 9.7 real (Testcontainers) e a API apontando para ele, compartilhados pela assembly.
/// Os testes criam dados com nomes únicos, então não dependem de ordem de execução.
/// </summary>
public sealed class MySqlApiFixture : IAsyncLifetime
{
    private readonly MySqlContainer _mysql = new MySqlBuilder("mysql:9.7")
        .WithCommand("--character-set-server=utf8mb4", "--collation-server=utf8mb4_0900_ai_ci")
        .Build();

    private ApiFactory? _factory;

    public WebApplicationFactory<Program> Factory => _factory ?? throw new InvalidOperationException("Fixture não iniciada.");

    public async ValueTask InitializeAsync()
    {
        await _mysql.StartAsync().ConfigureAwait(false);
        _factory = new ApiFactory(_mysql.GetConnectionString());

        // Força a criação do host, que aplica as migrations (Database:MigrateOnStartup).
        using var client = _factory.CreateClient();
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync().ConfigureAwait(false);
        }

        await _mysql.DisposeAsync().ConfigureAwait(false);
    }

    private sealed class ApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:Erp", connectionString);
            builder.UseSetting("Database:MigrateOnStartup", "true");
            builder.UseSetting("Logging:LogLevel:Default", "Warning");
            builder.UseSetting("Logging:LogLevel:Microsoft.EntityFrameworkCore", "Warning");
        }
    }
}
