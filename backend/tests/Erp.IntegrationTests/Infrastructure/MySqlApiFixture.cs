using System.Net.Http.Headers;
using System.Net.Http.Json;
using Erp.Application.Access;
using Erp.Domain.Access;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MySql;

[assembly: AssemblyFixture(typeof(Erp.IntegrationTests.Infrastructure.MySqlApiFixture))]

namespace Erp.IntegrationTests.Infrastructure;

/// <summary>
/// Um MySQL 9.7 real (Testcontainers) e a API apontando para ele, compartilhados pela assembly.
/// Os testes criam dados com nomes únicos, então não dependem de ordem de execução. Há um cliente
/// já autenticado para cada perfil (<see cref="ClientFor"/>).
/// </summary>
public sealed class MySqlApiFixture : IAsyncLifetime
{
    public const string AdminEmail = "admin@test.local";
    public const string Password = "Teste#12345";

    private readonly MySqlContainer _mysql = new MySqlBuilder("mysql:9.7")
        .WithCommand("--character-set-server=utf8mb4", "--collation-server=utf8mb4_0900_ai_ci")
        .Build();

    private readonly Dictionary<string, HttpClient> _clients = [];
    private ApiFactory? _factory;

    public WebApplicationFactory<Program> Factory => _factory ?? throw new InvalidOperationException("Fixture não iniciada.");

    /// <summary>Cliente autenticado com o perfil pedido (Admin, Operator ou Viewer).</summary>
    public HttpClient ClientFor(string role) => _clients[role];

    /// <summary>Cliente sem sessão, para login e testes de acesso anônimo.</summary>
    public HttpClient Anonymous() => Factory.CreateClient();

    public async ValueTask InitializeAsync()
    {
        await _mysql.StartAsync().ConfigureAwait(false);
        _factory = new ApiFactory(_mysql.GetConnectionString());

        // Criar o primeiro cliente sobe o host, que aplica as migrations e cria o Admin inicial.
        _clients[Roles.Admin] = await LoginAsync(AdminEmail, Password).ConfigureAwait(false);
        foreach (var role in new[] { Roles.Operator, Roles.Viewer })
        {
            var email = $"{role.ToLowerInvariant()}@test.local";
            using var response = await _clients[Roles.Admin].PostAsJsonAsync(
                new Uri("/api/v1/users", UriKind.Relative),
                new CreateUserRequest(email, $"Usuário {role}", role, Password)).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            _clients[role] = await LoginAsync(email, Password).ConfigureAwait(false);
        }
    }

    /// <summary>Faz login e devolve um cliente com o token de acesso no cabeçalho.</summary>
    public async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = Factory.CreateClient();
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative),
            new LoginRequest(email, password)).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var tokens = await response.Content.ReadFromJsonAsync<SessionTokensDto>().ConfigureAwait(false);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return client;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }

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
            builder.UseSetting("Auth:BootstrapAdmin:Email", AdminEmail);
            builder.UseSetting("Auth:BootstrapAdmin:Password", Password);
            // Os testes fazem muitos logins do mesmo "IP"; o limite real é coberto em produção.
            builder.UseSetting("Auth:LoginRateLimit:PermitLimit", "100000");
            builder.UseSetting("Logging:LogLevel:Default", "Warning");
            builder.UseSetting("Logging:LogLevel:Microsoft.EntityFrameworkCore", "Warning");
        }
    }
}
