using System.Net;
using System.Net.Http.Json;
using Erp.Application.Access;
using Erp.Application.Partners;
using Erp.Application.Products;
using Erp.Domain.Access;
using Erp.Domain.Partners;
using Erp.IntegrationTests.Infrastructure;
using static Erp.IntegrationTests.Infrastructure.ApiClient;

namespace Erp.IntegrationTests.Access;

/// <summary>
/// Critério de saída da Fase 3: o que cada perfil pode fazer. Anônimo não acessa nada; Viewer só lê;
/// Operator lê e grava cadastros; só Admin administra usuários.
/// </summary>
public sealed class AuthorizationTests(MySqlApiFixture fixture)
{
    public static TheoryData<string, HttpStatusCode> ReadProducts => new()
    {
        { Roles.Admin, HttpStatusCode.OK },
        { Roles.Operator, HttpStatusCode.OK },
        { Roles.Viewer, HttpStatusCode.OK },
    };

    public static TheoryData<string, HttpStatusCode> WriteCatalog => new()
    {
        { Roles.Admin, HttpStatusCode.Created },
        { Roles.Operator, HttpStatusCode.Created },
        { Roles.Viewer, HttpStatusCode.Forbidden },
    };

    public static TheoryData<string, HttpStatusCode> AdministerUsers => new()
    {
        { Roles.Admin, HttpStatusCode.OK },
        { Roles.Operator, HttpStatusCode.Forbidden },
        { Roles.Viewer, HttpStatusCode.Forbidden },
    };

    [Theory]
    [InlineData("/api/v1/products")]
    [InlineData("/api/v1/partners")]
    [InlineData("/api/v1/product-categories")]
    [InlineData("/api/v1/me")]
    [InlineData("/api/v1/users")]
    public async Task Anonymous_IsRejectedEverywhereExceptHealthAndLogin(string route)
    {
        using var client = fixture.Anonymous();

        using var response = await client.GetAsync(new Uri(route, UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(ReadProducts))]
    public async Task EveryRole_CanReadTheCatalog(string role, HttpStatusCode expected)
    {
        using var response = await fixture.ClientFor(role).GetAsync(new Uri("/api/v1/products?pageSize=1", UriKind.Relative), Ct);

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(WriteCatalog))]
    public async Task OnlyAdminAndOperator_CanCreateProducts(string role, HttpStatusCode expected)
    {
        using var response = await fixture.ClientFor(role).PostAsJsonAsync(
            new Uri("/api/v1/products", UriKind.Relative),
            new CreateProductRequest(null, $"Produto do {role}", null, null, null, 1, 2, 0),
            Ct);

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(WriteCatalog))]
    public async Task OnlyAdminAndOperator_CanCreatePartners(string role, HttpStatusCode expected)
    {
        using var response = await fixture.ClientFor(role).PostAsJsonAsync(
            new Uri("/api/v1/partners", UriKind.Relative),
            new CreatePartnerRequest(PartnerType.Customer, $"Cliente do {role}", NewCpf(), null, null),
            Ct);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Viewer_CannotChangeExistingRecords()
    {
        var partner = await fixture.ClientFor(Roles.Operator).CreatePartnerAsync();
        var viewer = fixture.ClientFor(Roles.Viewer);

        using var update = await viewer.PutAsJsonAsync(
            new Uri($"/api/v1/partners/{partner.Id}", UriKind.Relative),
            new UpdatePartnerRequest(partner.Type, "Outro nome", partner.Document, null, null, partner.Version),
            Ct);
        using var deactivate = await viewer.PostAsync(new Uri($"/api/v1/partners/{partner.Id}/deactivate", UriKind.Relative), null, Ct);
        using var deleteCategory = await viewer.DeleteAsync(new Uri("/api/v1/product-categories/1", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deactivate.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deleteCategory.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AdministerUsers))]
    public async Task OnlyAdmin_CanListUsers(string role, HttpStatusCode expected)
    {
        using var response = await fixture.ClientFor(role).GetAsync(new Uri("/api/v1/users", UriKind.Relative), Ct);

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Operator)]
    [InlineData(Roles.Viewer)]
    public async Task NonAdmin_CannotCreateUsers(string role)
    {
        using var response = await fixture.ClientFor(role).PostAsJsonAsync(
            new Uri("/api/v1/users", UriKind.Relative),
            new CreateUserRequest($"{Unique("x")}@test.local", "Intruso", Roles.Admin, MySqlApiFixture.Password),
            Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Operator)]
    [InlineData(Roles.Viewer)]
    public async Task EveryRole_ReadsOnlyItsOwnProfile(string role)
    {
        using var response = await fixture.ClientFor(role).GetAsync(new Uri("/api/v1/me", UriKind.Relative), Ct);

        response.EnsureSuccessStatusCode();
        var profile = (await response.Content.ReadFromJsonAsync<UserProfileDto>(Json, Ct))!;
        Assert.Equal(role, profile.Role);
    }

    [Fact]
    public async Task UnknownRoute_WithSession_Returns404ProblemDetails()
    {
        using var response = await fixture.ClientFor(Roles.Viewer).GetAsync(new Uri("/api/v1/does-not-exist", UriKind.Relative), Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
