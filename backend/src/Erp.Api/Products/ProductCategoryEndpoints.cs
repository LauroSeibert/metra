using Erp.Application.Common;
using Erp.Application.Products;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Erp.Api.Products;

internal static class ProductCategoryEndpoints
{
    public static RouteGroupBuilder MapProductCategoryEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/product-categories").WithTags("ProductCategories");

        group.MapGet("/", ListAsync).WithName("ListProductCategories").ProducesValidationProblem();
        group.MapGet("/{id:long}", GetAsync).WithName("GetProductCategory").ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/", CreateAsync)
            .WithName("CreateProductCategory")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapPut("/{id:long}", UpdateAsync)
            .WithName("UpdateProductCategory")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        group.MapDelete("/{id:long}", DeleteAsync)
            .WithName("DeleteProductCategory")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }

    private static async Task<Ok<PagedResult<ProductCategoryDto>>> ListAsync(
        ProductCategoryService service,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(search, page, pageSize, cancellationToken).ConfigureAwait(false));

    private static async Task<Ok<ProductCategoryDto>> GetAsync(
        long id,
        ProductCategoryService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAsync(id, cancellationToken).ConfigureAwait(false));

    private static async Task<Created<ProductCategoryDto>> CreateAsync(
        SaveProductCategoryRequest request,
        ProductCategoryService service,
        CancellationToken cancellationToken)
    {
        var category = await service.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return TypedResults.Created($"/api/v1/product-categories/{category.Id}", category);
    }

    private static async Task<Ok<ProductCategoryDto>> UpdateAsync(
        long id,
        SaveProductCategoryRequest request,
        ProductCategoryService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false));

    private static async Task<NoContent> DeleteAsync(long id, ProductCategoryService service, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return TypedResults.NoContent();
    }
}
