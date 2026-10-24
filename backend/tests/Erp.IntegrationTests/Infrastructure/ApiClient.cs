using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Erp.Application.Partners;
using Erp.Application.Products;
using Erp.Domain.Partners;

namespace Erp.IntegrationTests.Infrastructure;

/// <summary>Atalhos para os testes: criam o catálogo mínimo com nomes únicos e leem ProblemDetails.</summary>
internal static class ApiClient
{
    /// <summary>Mesmas opções da API: enums como texto ("Customer").</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>CPF válido e aleatório (dígitos verificadores calculados), nunca de uma pessoa real.</summary>
    public static string NewCpf()
    {
        var digits = string.Concat(Enumerable.Range(0, 9).Select(_ => (char)('0' + Random.Shared.Next(10))));
        digits += TaxDocument.CpfCheckDigit(digits);
        return digits + TaxDocument.CpfCheckDigit(digits);
    }

    /// <summary>CNPJ válido e aleatório; com <paramref name="alphanumeric"/>, no formato com letras (2026).</summary>
    public static string NewCnpj(bool alphanumeric = false)
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var root = string.Concat(Enumerable.Range(0, 8).Select(i => alphanumeric && i % 2 == 1
            ? letters[Random.Shared.Next(letters.Length)]
            : (char)('0' + Random.Shared.Next(10))));
        var body = root + "0001";
        body += TaxDocument.CnpjCheckDigit(body);
        return body + TaxDocument.CnpjCheckDigit(body);
    }

    public static async Task<PartnerDto> CreatePartnerAsync(
        this HttpClient client,
        PartnerType type = PartnerType.Customer,
        string? document = null,
        string? name = null)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri("/api/v1/partners", UriKind.Relative),
            new CreatePartnerRequest(type, name ?? Unique("Parceiro "), document ?? NewCpf(), null, null),
            Json,
            Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PartnerDto>(Json, Ct))!;
    }

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
