using Erp.Domain.Access;
using System.Net;
using System.Net.Http.Json;
using Erp.Application.Common;
using Erp.Application.Products;
using Erp.IntegrationTests.Infrastructure;
using static Erp.IntegrationTests.Infrastructure.ApiClient;

namespace Erp.IntegrationTests.Products;

public sealed class CatalogEndpointsTests(MySqlApiFixture fixture)
{
    private readonly HttpClient _client = fixture.ClientFor(Roles.Operator);

    [Fact]
    public async Task Category_DuplicateName_IgnoringCase_Returns409()
    {
        var category = await _client.CreateCategoryAsync();

        using var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1/product-categories", UriKind.Relative),
            new SaveProductCategoryRequest(category.Name.ToLowerInvariant(), null),
            Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal(ErrorCodes.ProductCategoryNameDuplicate, problem.Code());
    }

    [Fact]
    public async Task Category_InUse_CannotBeDeleted_AndShowsProductCount()
    {
        var category = await _client.CreateCategoryAsync();
        var unit = await _client.CreateUnitAsync();
        await _client.CreateProductAsync(category.Id, unit.Id);

        using var delete = await _client.DeleteAsync(new Uri($"/api/v1/product-categories/{category.Id}", UriKind.Relative), Ct);
        var problem = await delete.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal(ErrorCodes.ProductCategoryInUse, problem.Code());

        var reloaded = await _client.GetFromJsonAsync<ProductCategoryDto>(
            new Uri($"/api/v1/product-categories/{category.Id}", UriKind.Relative), Ct);
        Assert.Equal(1, reloaded!.ProductCount);
    }

    [Fact]
    public async Task Category_Unused_IsDeleted()
    {
        var category = await _client.CreateCategoryAsync();

        using var delete = await _client.DeleteAsync(new Uri($"/api/v1/product-categories/{category.Id}", UriKind.Relative), Ct);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        using var get = await _client.GetAsync(new Uri($"/api/v1/product-categories/{category.Id}", UriKind.Relative), Ct);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Category_UpdateWithStaleVersion_Returns409()
    {
        var category = await _client.CreateCategoryAsync();
        using (var first = await _client.PutAsJsonAsync(
            new Uri($"/api/v1/product-categories/{category.Id}", UriKind.Relative),
            new SaveProductCategoryRequest(Unique("REN"), category.Version),
            Ct))
        {
            first.EnsureSuccessStatusCode();
        }

        using var stale = await _client.PutAsJsonAsync(
            new Uri($"/api/v1/product-categories/{category.Id}", UriKind.Relative),
            new SaveProductCategoryRequest(Unique("REN"), category.Version),
            Ct);

        var problem = await stale.ReadProblemAsync(HttpStatusCode.Conflict);
        Assert.Equal(ErrorCodes.ConcurrencyConflict, problem.Code());
    }

    [Fact]
    public async Task Unit_DuplicateCode_Returns409_AndInUseCannotBeDeleted()
    {
        var category = await _client.CreateCategoryAsync();
        var unit = await _client.CreateUnitAsync();
        await _client.CreateProductAsync(category.Id, unit.Id);

        using var duplicate = await _client.PostAsJsonAsync(
            new Uri("/api/v1/units", UriKind.Relative),
            new SaveUnitRequest(unit.Code.ToLowerInvariant(), "Outra", null),
            Ct);
        Assert.Equal(ErrorCodes.UnitCodeDuplicate, (await duplicate.ReadProblemAsync(HttpStatusCode.Conflict)).Code());

        using var delete = await _client.DeleteAsync(new Uri($"/api/v1/units/{unit.Id}", UriKind.Relative), Ct);
        Assert.Equal(ErrorCodes.UnitInUse, (await delete.ReadProblemAsync(HttpStatusCode.Conflict)).Code());
    }

    [Fact]
    public async Task Categories_List_IsPaginated_WithMaximumOf100()
    {
        using var response = await _client.GetAsync(new Uri("/api/v1/product-categories?pageSize=101", UriKind.Relative), Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["pageSize"], problem.ErrorFields());
    }
}
