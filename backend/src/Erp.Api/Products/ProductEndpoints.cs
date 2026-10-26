using Erp.Api.Access;
using Erp.Application.Common;
using Erp.Application.Products;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Erp.Api.Products;

/// <summary>
/// Endpoints de produtos. Padrão de referência: endpoint fino que recebe a entrada, chama o caso de
/// uso e devolve DTO. Erros saem como exceções e viram ProblemDetails em ApiExceptionHandler.
/// </summary>
internal static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this RouteGroupBuilder api)
    {
        var group = api.MapGroup("/products").WithTags("Products");

        group.MapGet("/", ListAsync)
            .WithName("ListProducts")
            .WithSummary("Lista produtos com busca por prefixo de SKU ou nome, filtros e ordenação.")
            .ProducesValidationProblem();

        group.MapGet("/{id:long}", GetAsync)
            .WithName("GetProduct")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateAsync)
            .WithName("CreateProduct")
            .RequireWrite()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:long}", UpdateAsync)
            .WithName("UpdateProduct")
            .RequireWrite()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:long}/duplicate", DuplicateAsync)
            .WithName("DuplicateProduct")
            .WithSummary("Cria uma cópia do produto (nome com '(cópia)', SKU novo e o próximo código do sistema).")
            .RequireWrite()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:long}", DeleteAsync)
            .WithName("DeleteProduct")
            .WithSummary("Exclui o produto que nunca teve venda nem movimentação de estoque; os demais são inativados.")
            .RequireWrite()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:long}/activate", ActivateAsync)
            .WithName("ActivateProduct")
            .RequireWrite()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:long}/deactivate", DeactivateAsync)
            .WithName("DeactivateProduct")
            .RequireWrite()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return api;
    }

    private static async Task<Ok<PagedResult<ProductListItemDto>>> ListAsync(
        ProductService service,
        string? search,
        long? categoryId,
        bool? active,
        bool? lowStock,
        string? sort,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service
            .ListAsync(new ProductListQuery(search, categoryId, active, lowStock, sort, page, pageSize), cancellationToken)
            .ConfigureAwait(false));

    private static async Task<Ok<ProductDto>> GetAsync(long id, ProductService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAsync(id, cancellationToken).ConfigureAwait(false));

    private static async Task<Created<ProductDto>> CreateAsync(
        CreateProductRequest request,
        ProductService service,
        CancellationToken cancellationToken)
    {
        var product = await service.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return TypedResults.Created($"/api/v1/products/{product.Id}", product);
    }

    private static async Task<Ok<ProductDto>> UpdateAsync(
        long id,
        UpdateProductRequest request,
        ProductService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false));

    private static async Task<Created<ProductDto>> DuplicateAsync(long id, ProductService service, CancellationToken cancellationToken)
    {
        var product = await service.DuplicateAsync(id, cancellationToken).ConfigureAwait(false);
        return TypedResults.Created($"/api/v1/products/{product.Id}", product);
    }

    private static async Task<NoContent> DeleteAsync(long id, ProductService service, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<ProductDto>> ActivateAsync(long id, ProductService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.SetActiveAsync(id, true, cancellationToken).ConfigureAwait(false));

    private static async Task<Ok<ProductDto>> DeactivateAsync(long id, ProductService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.SetActiveAsync(id, false, cancellationToken).ConfigureAwait(false));
}
