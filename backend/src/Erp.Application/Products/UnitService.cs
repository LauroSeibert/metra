using Erp.Application.Abstractions;
using Erp.Application.Common;
using Erp.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Products;

public sealed class UnitService(IErpDbContext db)
{
    public async Task<PagedResult<UnitDto>> ListAsync(
        string? search,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var pageRequest = PageRequest.From(page, pageSize);
        var units = db.Units.AsNoTracking();

        var pattern = SearchPattern.Prefix(search);
        if (pattern is not null)
        {
            units = units.Where(u =>
                EF.Functions.Like(u.Code, pattern, SearchPattern.EscapeCharacter)
                || EF.Functions.Like(u.Name, pattern, SearchPattern.EscapeCharacter));
        }

        return await units
            .OrderBy(u => u.Code)
            .ThenBy(u => u.Id)
            .Select(u => new UnitDto(u.Id, u.Code, u.Name, db.Products.Count(p => p.UnitId == u.Id), u.Version))
            .ToPagedResultAsync(pageRequest, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<UnitDto> GetAsync(long id, CancellationToken cancellationToken) =>
        await db.Units.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new UnitDto(u.Id, u.Code, u.Name, db.Products.Count(p => p.UnitId == u.Id), u.Version))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
        ?? throw NotFound();

    public async Task<UnitDto> CreateAsync(SaveUnitRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var unit = Unit.Create(request.Code, request.Name);
        await EnsureCodeIsFreeAsync(unit.Code, null, cancellationToken).ConfigureAwait(false);

        db.Units.Add(unit);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetAsync(unit.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UnitDto> UpdateAsync(long id, SaveUnitRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var unit = await FindTrackedAsync(id, cancellationToken).ConfigureAwait(false);
        if (request.Version is { } version && unit.Version != version)
        {
            throw ConflictException.StaleVersion();
        }

        unit.Update(request.Code, request.Name);
        await EnsureCodeIsFreeAsync(unit.Code, unit.Id, cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetAsync(unit.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Exclusão física, permitida só sem produtos vinculados.</summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var unit = await FindTrackedAsync(id, cancellationToken).ConfigureAwait(false);
        if (await db.Products.AnyAsync(p => p.UnitId == id, cancellationToken).ConfigureAwait(false))
        {
            throw new ConflictException(ErrorCodes.UnitInUse, "A unidade tem produtos e não pode ser excluída.");
        }

        db.Units.Remove(unit);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<Unit> FindTrackedAsync(long id, CancellationToken cancellationToken) =>
        await db.Units.FirstOrDefaultAsync(u => u.Id == id, cancellationToken).ConfigureAwait(false)
        ?? throw NotFound();

    private async Task EnsureCodeIsFreeAsync(string code, long? exceptId, CancellationToken cancellationToken)
    {
        var taken = await db.Units
            .AnyAsync(u => u.Code == code && (exceptId == null || u.Id != exceptId), cancellationToken)
            .ConfigureAwait(false);
        if (taken)
        {
            throw new ConflictException(ErrorCodes.UnitCodeDuplicate, $"Já existe a unidade {code}.");
        }
    }

    private static NotFoundException NotFound() => new(ErrorCodes.UnitNotFound, "Unidade não encontrada.");
}
