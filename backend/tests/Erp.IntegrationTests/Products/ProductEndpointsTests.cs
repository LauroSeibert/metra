using System.Net;
using System.Net.Http.Json;
using Erp.Application.Common;
using Erp.Application.Products;
using Erp.Infrastructure.Persistence;
using Erp.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Erp.IntegrationTests.Infrastructure.ApiClient;

namespace Erp.IntegrationTests.Products;

public sealed class ProductEndpointsTests(MySqlApiFixture fixture)
{
    private readonly HttpClient _client = fixture.Factory.CreateClient();

    [Fact]
    public async Task Create_NormalizesSku_AndStartsAtVersionOne()
    {
        var category = await _client.CreateCategoryAsync();
        var unit = await _client.CreateUnitAsync();
        var sku = Unique("new-");

        var product = await _client.CreateProductAsync(category.Id, unit.Id, sku: $"  {sku.ToLowerInvariant()} ");

        Assert.Equal(sku, product.Sku);
        Assert.Equal(1, product.Version);
        Assert.True(product.Active);
        Assert.Equal(category.Name, product.CategoryName);
        Assert.Equal(DateTimeKind.Utc, product.CreatedAt.Kind);
    }

    [Fact]
    public async Task Create_WithoutSku_GeneratesFromCategoryAndId()
    {
        var category = await _client.CreateCategoryAsync(Unique("Ótica "));
        var unit = await _client.CreateUnitAsync();

        var product = await CreateWithoutSkuAsync(category.Id, unit.Id);

        Assert.Equal($"OTI-{product.Id:D6}", product.Sku);
    }

    [Fact]
    public async Task Create_WithoutCategoryAndUnit_GeneratesFallbackSku()
    {
        using var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1/products", UriKind.Relative),
            new CreateProductRequest(null, "Avulso", null, null, null, 1, 2, 0),
            Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = (await response.Content.ReadFromJsonAsync<ProductDto>(Ct))!;
        Assert.Equal($"PRD-{product.Id:D6}", product.Sku);
        Assert.Null(product.CategoryId);
        Assert.Null(product.CategoryName);
        Assert.Null(product.UnitId);
        Assert.Null(product.UnitCode);

        var listed = await ListAsync($"search={product.Sku}");
        Assert.Null(Assert.Single(listed.Items).CategoryName);
    }

    [Fact]
    public async Task GeneratedSku_WhenCodeWasTypedByAnotherProduct_AddsSuffix()
    {
        var category = await _client.CreateCategoryAsync(Unique("Zinco "));
        var unit = await _client.CreateUnitAsync();
        var product = await _client.CreateProductAsync(category.Id, unit.Id);
        await _client.CreateProductAsync(category.Id, unit.Id, sku: $"ZIN-{product.Id:D6}");

        using var response = await _client.PutAsJsonAsync(
            new Uri($"/api/v1/products/{product.Id}", UriKind.Relative),
            UpdateFrom(product) with { Sku = null },
            Ct);

        response.EnsureSuccessStatusCode();
        var updated = (await response.Content.ReadFromJsonAsync<ProductDto>(Ct))!;
        Assert.Equal($"ZIN-{product.Id:D6}-2", updated.Sku);
    }

    [Fact]
    public async Task Update_WithBlankSku_GeneratesFromCategoryAndId()
    {
        var category = await _client.CreateCategoryAsync(Unique("Lápis "));
        var unit = await _client.CreateUnitAsync();
        var product = await _client.CreateProductAsync(category.Id, unit.Id);

        using var response = await _client.PutAsJsonAsync(
            new Uri($"/api/v1/products/{product.Id}", UriKind.Relative),
            UpdateFrom(product) with { Sku = "  " },
            Ct);

        response.EnsureSuccessStatusCode();
        var updated = (await response.Content.ReadFromJsonAsync<ProductDto>(Ct))!;
        Assert.Equal($"LAP-{product.Id:D6}", updated.Sku);
    }

    [Fact]
    public async Task Create_DuplicateSku_IgnoringCase_Returns409()
    {
        var category = await _client.CreateCategoryAsync();
        var unit = await _client.CreateUnitAsync();
        var existing = await _client.CreateProductAsync(category.Id, unit.Id);

        using var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1/products", UriKind.Relative),
            new CreateProductRequest(existing.Sku.ToLowerInvariant(), "Outro", null, category.Id, unit.Id, 1, 1, 0),
            Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal(ErrorCodes.ProductSkuDuplicate, problem.Code());
    }

    [Fact]
    public async Task Create_InvalidData_Returns400_WithFieldErrors()
    {
        using var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1/products", UriKind.Relative),
            new CreateProductRequest("OK-1", "", null, 1, 1, -1, -5, 0),
            Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(ErrorCodes.ValidationFailed, problem.Code());
        Assert.Equal(["costPrice", "name", "salePrice"], problem.ErrorFields());
    }

    [Fact]
    public async Task Create_UnknownCategoryAndUnit_Returns400()
    {
        using var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1/products", UriKind.Relative),
            new CreateProductRequest(Unique("REF-"), "Produto", null, long.MaxValue, long.MaxValue, 1, 1, 0),
            Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["categoryId", "unitId"], problem.ErrorFields());
    }

    [Fact]
    public async Task Get_Unknown_Returns404()
    {
        using var response = await _client.GetAsync(new Uri("/api/v1/products/999999999", UriKind.Relative), Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.NotFound);
        Assert.Equal(ErrorCodes.ProductNotFound, problem.Code());
    }

    [Fact]
    public async Task Update_ChangesData_AndIncrementsVersion()
    {
        var category = await _client.CreateCategoryAsync();
        var unit = await _client.CreateUnitAsync();
        var product = await _client.CreateProductAsync(category.Id, unit.Id);

        using var response = await _client.PutAsJsonAsync(
            new Uri($"/api/v1/products/{product.Id}", UriKind.Relative),
            UpdateFrom(product, name: "Nome novo", salePrice: 12.34m),
            Ct);

        response.EnsureSuccessStatusCode();
        var updated = (await response.Content.ReadFromJsonAsync<ProductDto>(Ct))!;
        Assert.Equal("Nome novo", updated.Name);
        Assert.Equal(12.34m, updated.SalePrice);
        Assert.Equal(2, updated.Version);
        Assert.True(updated.UpdatedAt >= product.UpdatedAt);
    }

    [Fact]
    public async Task Update_WithStaleVersion_Returns409()
    {
        var category = await _client.CreateCategoryAsync();
        var unit = await _client.CreateUnitAsync();
        var product = await _client.CreateProductAsync(category.Id, unit.Id);
        using (var first = await _client.PutAsJsonAsync(
            new Uri($"/api/v1/products/{product.Id}", UriKind.Relative), UpdateFrom(product, name: "Primeira"), Ct))
        {
            first.EnsureSuccessStatusCode();
        }

        using var stale = await _client.PutAsJsonAsync(
            new Uri($"/api/v1/products/{product.Id}", UriKind.Relative),
            UpdateFrom(product, name: "Segunda"),
            Ct);

        var problem = await stale.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal(ErrorCodes.ConcurrencyConflict, problem.Code());
    }

    [Fact]
    public async Task ConcurrentSave_IsRejectedByVersionToken()
    {
        var category = await _client.CreateCategoryAsync();
        var unit = await _client.CreateUnitAsync();
        var created = await _client.CreateProductAsync(category.Id, unit.Id);

        using var scopeA = fixture.Factory.Services.CreateScope();
        using var scopeB = fixture.Factory.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<ErpDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<ErpDbContext>();
        var productA = await dbA.Products.SingleAsync(p => p.Id == created.Id, Ct);
        var productB = await dbB.Products.SingleAsync(p => p.Id == created.Id, Ct);

        productA.Deactivate();
        await dbA.SaveChangesAsync(Ct);
        productB.Deactivate();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync(Ct));
    }

    [Fact]
    public async Task Deactivate_ThenActivate_AndProductIsNeverDeleted()
    {
        var category = await _client.CreateCategoryAsync();
        var unit = await _client.CreateUnitAsync();
        var product = await _client.CreateProductAsync(category.Id, unit.Id);

        using var deactivate = await _client.PostAsync(new Uri($"/api/v1/products/{product.Id}/deactivate", UriKind.Relative), null, Ct);
        var inactive = (await deactivate.Content.ReadFromJsonAsync<ProductDto>(Ct))!;
        Assert.False(inactive.Active);

        var inactiveList = await ListAsync($"categoryId={category.Id}&active=false");
        Assert.Equal([product.Id], inactiveList.Items.Select(p => p.Id));

        using var activate = await _client.PostAsync(new Uri($"/api/v1/products/{product.Id}/activate", UriKind.Relative), null, Ct);
        Assert.True((await activate.Content.ReadFromJsonAsync<ProductDto>(Ct))!.Active);

        using var delete = await _client.DeleteAsync(new Uri($"/api/v1/products/{product.Id}", UriKind.Relative), Ct);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, delete.StatusCode);
    }

    [Fact]
    public async Task List_SearchByNamePrefix_PaginatesAndSortsByName()
    {
        var category = await _client.CreateCategoryAsync();
        var unit = await _client.CreateUnitAsync();
        var prefix = Unique("Z");
        await _client.CreateProductAsync(category.Id, unit.Id, name: $"{prefix} Charlie");
        await _client.CreateProductAsync(category.Id, unit.Id, name: $"{prefix} alfa");
        await _client.CreateProductAsync(category.Id, unit.Id, name: $"{prefix} Bravo");
        await _client.CreateProductAsync(category.Id, unit.Id, name: $"Outro {prefix}");

        var firstPage = await ListAsync($"search={prefix.ToLowerInvariant()}&pageSize=2");
        var secondPage = await ListAsync($"search={prefix}&pageSize=2&page=2");

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal([$"{prefix} alfa", $"{prefix} Bravo"], firstPage.Items.Select(p => p.Name));
        Assert.Equal([$"{prefix} Charlie"], secondPage.Items.Select(p => p.Name));
    }

    [Fact]
    public async Task List_SearchBySkuPrefix_AndSortBySalePriceDescending()
    {
        var category = await _client.CreateCategoryAsync();
        var unit = await _client.CreateUnitAsync();
        var prefix = Unique("PX");
        var cheap = await _client.CreateProductAsync(category.Id, unit.Id, sku: $"{prefix}-1", salePrice: 1m);
        var expensive = await _client.CreateProductAsync(category.Id, unit.Id, sku: $"{prefix}-2", salePrice: 99m);

        var result = await ListAsync($"search={prefix}&sort=-salePrice");

        Assert.Equal([expensive.Id, cheap.Id], result.Items.Select(p => p.Id));
    }

    [Fact]
    public async Task List_WildcardInSearch_IsTreatedAsLiteral()
    {
        var category = await _client.CreateCategoryAsync();
        var unit = await _client.CreateUnitAsync();
        await _client.CreateProductAsync(category.Id, unit.Id, name: "Camiseta comum");
        var literal = await _client.CreateProductAsync(category.Id, unit.Id, name: "100% algodão");

        var percent = await ListAsync($"categoryId={category.Id}&search=%25");
        var underscore = await ListAsync($"categoryId={category.Id}&search=_");
        var hundred = await ListAsync($"categoryId={category.Id}&search=100%25");

        Assert.Equal(0, percent.TotalCount);
        Assert.Equal(0, underscore.TotalCount);
        Assert.Equal([literal.Id], hundred.Items.Select(p => p.Id));
    }

    [Theory]
    [InlineData("pageSize=101", "pageSize")]
    [InlineData("page=0", "page")]
    [InlineData("sort=cost_price", "sort")]
    public async Task List_InvalidParameters_Returns400(string query, string field)
    {
        using var response = await _client.GetAsync(new Uri($"/api/v1/products?{query}", UriKind.Relative), Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal([field], problem.ErrorFields());
    }

    private async Task<PagedResult<ProductListItemDto>> ListAsync(string query)
    {
        using var response = await _client.GetAsync(new Uri($"/api/v1/products?{query}", UriKind.Relative), Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PagedResult<ProductListItemDto>>(Ct))!;
    }

    private async Task<ProductDto> CreateWithoutSkuAsync(long categoryId, long unitId)
    {
        using var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1/products", UriKind.Relative),
            new CreateProductRequest(null, "Sem código", null, categoryId, unitId, 1, 2, 0),
            Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProductDto>(Ct))!;
    }

    private static UpdateProductRequest UpdateFrom(ProductDto product, string? name = null, decimal? salePrice = null) =>
        new(
            product.Sku,
            name ?? product.Name,
            product.Description,
            product.CategoryId,
            product.UnitId,
            product.CostPrice,
            salePrice ?? product.SalePrice,
            product.MinStock,
            product.Version);
}
