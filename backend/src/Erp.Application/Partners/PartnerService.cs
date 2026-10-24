using System.Linq.Expressions;
using Erp.Application.Abstractions;
using Erp.Application.Common;
using Erp.Domain.Partners;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Partners;

/// <summary>Casos de uso de clientes e fornecedores. Mesma estrutura do módulo de produtos.</summary>
public sealed class PartnerService(IErpDbContext db)
{
    public static readonly IReadOnlyCollection<string> SortFields = ["name", "document", "createdAt"];

    public async Task<PagedResult<PartnerListItemDto>> ListAsync(PartnerListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var page = PageRequest.From(query.Page, query.PageSize);
        var sort = SortRequest.Parse(query.Sort, "name", SortFields);

        var partners = db.Partners.AsNoTracking();

        // Cliente inclui "ambos"; fornecedor também. Filtrar "ambos" traz só os que têm os dois papéis.
        partners = query.Type switch
        {
            PartnerType.Customer => partners.Where(p => p.Type == PartnerType.Customer || p.Type == PartnerType.Both),
            PartnerType.Supplier => partners.Where(p => p.Type == PartnerType.Supplier || p.Type == PartnerType.Both),
            PartnerType.Both => partners.Where(p => p.Type == PartnerType.Both),
            _ => partners,
        };

        if (query.Active is { } active)
        {
            partners = partners.Where(p => p.Active == active);
        }

        var namePattern = SearchPattern.Prefix(query.Search);
        if (namePattern is null)
        {
            return await Order(partners, sort)
                .Select(ToListItem)
                .ToPagedResultAsync(page, cancellationToken)
                .ConfigureAwait(false);
        }

        // Busca por prefixo do nome ou do documento (digitado com ou sem pontuação). Mesmo UNION estreito
        // da busca de produtos, pelo mesmo motivo: OR entre colunas engana o otimizador com ORDER BY + LIMIT.
        var keys = partners
            .Where(p => EF.Functions.Like(p.Name, namePattern, SearchPattern.EscapeCharacter))
            .Select(ToSearchKey);
        var documentPattern = SearchPattern.Prefix(TaxDocument.Normalize(query.Search!));
        if (documentPattern is not null)
        {
            keys = keys.Union(partners
                .Where(p => EF.Functions.Like(p.Document, documentPattern, SearchPattern.EscapeCharacter))
                .Select(ToSearchKey));
        }

        var total = await keys.CountAsync(cancellationToken).ConfigureAwait(false);
        var pageKeys = OrderKeys(keys, sort).Skip(page.Skip).Take(page.PageSize);
        var pagePartners = from key in pageKeys
                           join partner in db.Partners.AsNoTracking() on key.Id equals partner.Id
                           select partner;

        var items = await Order(pagePartners, sort)
            .Select(ToListItem)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<PartnerListItemDto>(items, page.Page, page.PageSize, total);
    }

    public async Task<PartnerDto> GetAsync(long id, CancellationToken cancellationToken) =>
        await db.Partners.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PartnerDto(
                p.Id,
                p.Type,
                p.Name,
                p.Document,
                p.DocumentKind,
                p.Email,
                p.Phone,
                p.Active,
                p.Version,
                p.CreatedAt,
                p.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
        ?? throw NotFound();

    public async Task<PartnerDto> CreateAsync(CreatePartnerRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var partner = Partner.Create(new PartnerData(request.Type, request.Name, request.Document, request.Email, request.Phone));
        await EnsureDocumentIsFreeAsync(partner.Document, null, cancellationToken).ConfigureAwait(false);

        db.Partners.Add(partner);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetAsync(partner.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<PartnerDto> UpdateAsync(long id, UpdatePartnerRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var partner = await FindTrackedAsync(id, cancellationToken).ConfigureAwait(false);
        if (partner.Version != request.Version)
        {
            throw ConflictException.StaleVersion();
        }

        partner.Update(new PartnerData(request.Type, request.Name, request.Document, request.Email, request.Phone));
        await EnsureDocumentIsFreeAsync(partner.Document, partner.Id, cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetAsync(partner.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Ativa ou inativa. Parceiro nunca é excluído (ver docs/modules/partners.md).</summary>
    public async Task<PartnerDto> SetActiveAsync(long id, bool active, CancellationToken cancellationToken)
    {
        var partner = await FindTrackedAsync(id, cancellationToken).ConfigureAwait(false);
        if (partner.Active != active)
        {
            if (active)
            {
                partner.Activate();
            }
            else
            {
                partner.Deactivate();
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return await GetAsync(partner.Id, cancellationToken).ConfigureAwait(false);
    }

    private static readonly Expression<Func<Partner, PartnerListItemDto>> ToListItem = p => new PartnerListItemDto(
        p.Id,
        p.Type,
        p.Name,
        p.Document,
        p.DocumentKind,
        p.Email,
        p.Phone,
        p.Active,
        p.UpdatedAt);

    private static readonly Expression<Func<Partner, PartnerSearchKey>> ToSearchKey = p => new PartnerSearchKey
    {
        Id = p.Id,
        Name = p.Name,
        Document = p.Document,
        CreatedAt = p.CreatedAt,
    };

    private static IQueryable<Partner> Order(IQueryable<Partner> partners, SortRequest sort) =>
        (sort.Field, sort.Descending) switch
        {
            ("document", false) => partners.OrderBy(p => p.Document),
            ("document", true) => partners.OrderByDescending(p => p.Document),
            ("createdAt", false) => partners.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
            ("createdAt", true) => partners.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id),
            (_, true) => partners.OrderByDescending(p => p.Name).ThenByDescending(p => p.Id),
            _ => partners.OrderBy(p => p.Name).ThenBy(p => p.Id),
        };

    private static IQueryable<PartnerSearchKey> OrderKeys(IQueryable<PartnerSearchKey> keys, SortRequest sort) =>
        (sort.Field, sort.Descending) switch
        {
            ("document", false) => keys.OrderBy(k => k.Document),
            ("document", true) => keys.OrderByDescending(k => k.Document),
            ("createdAt", false) => keys.OrderBy(k => k.CreatedAt).ThenBy(k => k.Id),
            ("createdAt", true) => keys.OrderByDescending(k => k.CreatedAt).ThenByDescending(k => k.Id),
            (_, true) => keys.OrderByDescending(k => k.Name).ThenByDescending(k => k.Id),
            _ => keys.OrderBy(k => k.Name).ThenBy(k => k.Id),
        };

    private async Task<Partner> FindTrackedAsync(long id, CancellationToken cancellationToken) =>
        await db.Partners.FirstOrDefaultAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false)
        ?? throw NotFound();

    private async Task EnsureDocumentIsFreeAsync(string document, long? exceptId, CancellationToken cancellationToken)
    {
        var taken = await db.Partners
            .AnyAsync(p => p.Document == document && (exceptId == null || p.Id != exceptId), cancellationToken)
            .ConfigureAwait(false);
        if (taken)
        {
            throw new ConflictException(
                ErrorCodes.PartnerDocumentDuplicate,
                $"Já existe um parceiro com o documento {TaxDocument.Format(document)}.");
        }
    }

    private static NotFoundException NotFound() => new(ErrorCodes.PartnerNotFound, "Parceiro não encontrado.");
}

/// <summary>Projeção estreita usada no UNION da busca: só o id e as colunas de ordenação.</summary>
internal sealed class PartnerSearchKey
{
    public long Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Document { get; init; } = string.Empty;

    public DateTime CreatedAt { get; init; }
}
