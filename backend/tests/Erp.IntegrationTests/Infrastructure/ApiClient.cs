using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Erp.Application.Products;

namespace Erp.IntegrationTests.Infrastructure;

/// <summary>Atalhos para os testes: criam o catálogo mínimo com nomes únicos e leem ProblemDetails.</summary>
internal static class ApiClient
{
    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static string Unique(string prefix) => $"{prefix}{Guid.NewGuid():N}"[..Math.Min(prefix.Length + 12, 40)].ToUpperInvariant();

    public static async Task<ProductCategoryDto> CreateCategoryAsync(this HttpClient client, string? name = null)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/product-categories", UriKind.Relative),
            new SaveProductCategoryRequest(name ?? Unique("CAT"), null),
            Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProductCategoryDto>(Ct))!;
    }

    public static async Task<UnitDto> CreateUnitAsync(this HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/units", UriKind.Relative),
            new SaveUnitRequest(Unique("U")[..8], "Unidade de teste", null),
            Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UnitDto>(Ct))!;
    }

    public static async Task<ProductDto> CreateProductAsync(
        this HttpClient client,
        long categoryId,
        long unitId,
        string? sku = null,
        string name = "Produto de teste",
        decimal salePrice = 10m)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/products", UriKind.Relative),
            new CreateProductRequest(sku ?? Unique("SKU-"), name, null, categoryId, unitId, 5m, salePrice, 1m),
            Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductDto>(Ct))!;
    }

    public static async Task<JsonElement> ReadProblemAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return document.RootElement.Clone();
    }

    public static string Code(this JsonElement problem) => problem.GetProperty("code").GetString()!;

    public static string[] ErrorFields(this JsonElement problem) =>
        [.. problem.GetProperty("errors").EnumerateObject().Select(property => property.Name).Order()];
}
