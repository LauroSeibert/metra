using Erp.Application.Abstractions;
using Erp.Application.Common;
using Erp.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Finance;

/// <summary>Categorias financeiras. Mesmo padrão das categorias de produto.</summary>
public sealed class FinancialCategoryService(IErpDbContext db)
{
    public async Task<PagedResult<FinancialCategoryDto>> ListAsync(
        string? search,
        FinancialCategoryKind? kind,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var pageRequest = PageRequest.From(page, pageSize);
        var categories = db.FinancialCategories.AsNoTracking();
        if (kind is { } value)
        {
            categories = categories.Where(c => c.Kind == value);
        }

        var pattern = SearchPattern.Prefix(search);
        if (pattern is not null)
        {
            categories = categories.Where(c => EF.Functions.Like(c.Name, pattern, SearchPattern.EscapeCharacter));
        }

        return await categories
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Select(c => new FinancialCategoryDto(c.Id, c.Name, c.Kind, db.FinancialEntries.Count(e => e.CategoryId == c.Id), c.Version))
            .ToPagedResultAsync(pageRequest, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<FinancialCategoryDto> GetAsync(long id, CancellationToken cancellationToken) =>
        await db.FinancialCategories.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new FinancialCategoryDto(c.Id, c.Name, c.Kind, db.FinancialEntries.Count(e => e.CategoryId == c.Id), c.Version))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
        ?? throw NotFound();

    public async Task<FinancialCategoryDto> CreateAsync(SaveFinancialCategoryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var category = FinancialCategory.Create(request.Name, request.Kind);
        await EnsureNameIsFreeAsync(category, null, cancellationToken).ConfigureAwait(false);

        db.FinancialCategories.Add(category);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetAsync(category.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<FinancialCategoryDto> UpdateAsync(long id, SaveFinancialCategoryRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var category = await FindTrackedAsync(id, cancellationToken).ConfigureAwait(false);
        if (request.Version is { } version && category.Version != version)
        {
            throw ConflictException.StaleVersion();
        }

        // Com lançamentos, o tipo não muda: a receita viraria despesa para lançamentos já feitos.
        if (category.Kind != request.Kind
            && await db.FinancialEntries.AnyAsync(e => e.CategoryId == id, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException(ErrorCodes.FinancialCategoryInUse, "A categoria tem lançamentos; o tipo não pode mudar.");
        }

        category.Update(request.Name, request.Kind);
        await EnsureNameIsFreeAsync(category, category.Id, cancellationToken).ConfigureAwait(false);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetAsync(category.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var category = await FindTrackedAsync(id, cancellationToken).ConfigureAwait(false);
        if (await db.FinancialEntries.AnyAsync(e => e.CategoryId == id, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException(ErrorCodes.FinancialCategoryInUse, "A categoria tem lançamentos e não pode ser excluída.");
        }

        db.FinancialCategories.Remove(category);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<FinancialCategory> FindTrackedAsync(long id, CancellationToken cancellationToken) =>
        await db.FinancialCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false)
        ?? throw NotFound();

    private async Task EnsureNameIsFreeAsync(FinancialCategory category, long? exceptId, CancellationToken cancellationToken)
    {
        var taken = await db.FinancialCategories
            .AnyAsync(c => c.Name == category.Name && c.Kind == category.Kind && (exceptId == null || c.Id != exceptId), cancellationToken)
            .ConfigureAwait(false);
        if (taken)
        {
            throw new ConflictException(ErrorCodes.FinancialCategoryNameDuplicate, $"Já existe a categoria {category.Name} desse tipo.");
        }
    }

    private static NotFoundException NotFound() =>
        new(ErrorCodes.FinancialCategoryNotFound, "Categoria financeira não encontrada.");
}
