using Erp.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Common;

/// <summary>Página pedida pelo cliente. Toda listagem é paginada no servidor, com no máximo 100 itens.</summary>
public sealed record PageRequest(int Page, int PageSize)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Skip => (Page - 1) * PageSize;

    public static PageRequest From(int? page, int? pageSize)
    {
        var request = new PageRequest(page ?? 1, pageSize ?? DefaultPageSize);
        var errors = new ValidationErrors();
        if (request.Page < 1)
        {
            errors.Add("page", "A página começa em 1.");
        }

        if (request.PageSize is < 1 or > MaxPageSize)
        {
            errors.Add("pageSize", $"Use entre 1 e {MaxPageSize} itens por página.");
        }

        errors.ThrowIfAny();
        return request;
    }
}

/// <summary>Resultado paginado devolvido pelas listagens.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public static class PagingExtensions
{
    /// <summary>Conta e busca a página. A consulta já deve estar ordenada de forma estável (com desempate por Id).</summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);
        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
        return new PagedResult<T>(items, page.Page, page.PageSize, total);
    }
}
