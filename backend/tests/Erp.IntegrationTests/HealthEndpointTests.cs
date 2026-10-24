using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Erp.IntegrationTests;

public sealed class HealthEndpointTests : IClassFixture<HealthEndpointTests.ApiFactory>
{
    private readonly ApiFactory _factory;

    public HealthEndpointTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Health_ReturnsOk_WithoutDatabase()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/health", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnknownRoute_WithoutSession_Returns401ProblemDetails()
    {
        // Sem sessão, a API não revela quais rotas existem: tudo fora de health e login pede autenticação.
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/does-not-exist", UriKind.Relative), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    public sealed class ApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Connection string fictícia: /health não toca o banco. Testes com banco usarão Testcontainers.
            builder.UseSetting("ConnectionStrings:Erp", "Server=localhost;Port=1;Database=unused;User=unused;Password=unused");
        }
    }
}
