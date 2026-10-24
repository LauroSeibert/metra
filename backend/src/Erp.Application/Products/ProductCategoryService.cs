using Erp.Application.Abstractions;
using Erp.Application.Common;
using Erp.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Products;

public sealed class ProductCategoryService(IErpDbContext db)
{
    public async Task<PagedResult<ProductCategoryDto>> ListAsync(
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var pageRequest = PageRequest.From(page, pageSize);
        var categories = db.ProductCategories.AsNoTracking();

        var pattern = SearchPattern.Prefix(search);
        if (pattern is not null)
        {
            categories = categories.Where(c => EF.Functions.Like(c.Name, pattern, SearchPattern.EscapeCharacter));
        }

        return await categories
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Select(c => new ProductCategoryDto(
                c.Id,
                c.Name,
                db.Products.Count(p => p.CategoryId == c.Id),
                c.Version))
            .ToPagedResultAsync(pageRequest, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<ProductCategoryDto> GetAsync(long id, CancellationToken cancellationToken) =>
        await db.ProductCategories.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ProductCategoryDto(c.Id, c.Name, db.Products.Count(p => p.CategoryId == c.Id), c.Version))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
        ?? throw NotFound();

    public async Task<ProductCategoryDto> CreateAsync(SaveProductCategoryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var category = ProductCategory.Create(request.Name);
        await EnsureNameIsFreeAsync(category.Name, null, cancellationToken).ConfigureAwait(false);

        db.ProductCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetAsync(category.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProductCategoryDto> UpdateAsync(long id, SaveProductCategoryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var category = await FindTrackedAsync(id, cancellationToken).ConfigureAwait(false);
        if (request.Version is { } version && category.Version != version)
        {
            throw ConflictException.StaleVersion();
        }

        category.Rename(request.Name);
        await EnsureNameIsFreeAsync(category.Name, category.Id, cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetAsync(category.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Exclusão física, permitida só sem produtos vinculados.</summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var category = await FindTrackedAsync(id, cancellationToken).ConfigureAwait(false);
        if (await db.Products.AnyAsync(p => p.CategoryId == id, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException(ErrorCodes.ProductCategoryInUse, "A categoria tem produtos e não pode ser excluída.");
        }

        db.ProductCategories.Remove(category);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProductCategory> FindTrackedAsync(long id, CancellationToken cancellationToken) =>
        await db.ProductCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false)
        ?? throw NotFound();

    private async Task EnsureNameIsFreeAsync(string name, long? exceptId, CancellationToken cancellationToken)
    {
        var taken = await db.ProductCategories
            .AnyAsync(c => c.Name == name && (exceptId == null || c.Id != exceptId), cancellationToken)
            .ConfigureAwait(false);
        if (taken)
        {
            throw new ConflictException(ErrorCodes.ProductCategoryNameDuplicate, $"Já existe a categoria {name}.");
        }
    }

    private static NotFoundException NotFound() => new(ErrorCodes.ProductCategoryNotFound, "Categoria não encontrada.");
}
