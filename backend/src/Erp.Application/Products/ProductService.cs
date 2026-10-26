using System.Linq.Expressions;
using Erp.Application.Abstractions;
using Erp.Application.Common;
using Erp.Domain.Common;
using Erp.Domain.Products;
using Erp.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Products;

/// <summary>Casos de uso de produtos. Módulo de referência: novos módulos seguem esta estrutura.</summary>
public sealed class ProductService(IErpDbContext db)
{
    public static readonly IReadOnlyCollection<string> SortFields = ["code", "name", "sku", "salePrice", "createdAt"];

    public async Task<PagedResult<ProductListItemDto>> ListAsync(ProductListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var page = PageRequest.From(query.Page, query.PageSize);
        var sort = SortRequest.Parse(string.IsNullOrWhiteSpace(query.Sort) ? "-code" : query.Sort, "code", SortFields);

        var products = db.Products.AsNoTracking();

        if (query.CategoryId is { } categoryId)
        {
            products = products.Where(p => p.CategoryId == categoryId);
        }

        if (query.Active is { } active)
        {
            products = products.Where(p => p.Active == active);
        }

        if (query.LowStock is { } lowStock)
        {
            // Produto sem linha de saldo conta como zero.
            products = lowStock
                ? products.Where(p => (db.StockBalances.Where(b => b.ProductId == p.Id).Select(b => (decimal?)b.Quantity).FirstOrDefault() ?? 0) < p.MinStock)
                : products.Where(p => (db.StockBalances.Where(b => b.ProductId == p.Id).Select(b => (decimal?)b.Quantity).FirstOrDefault() ?? 0) >= p.MinStock);
        }

        var pattern = SearchPattern.Contains(query.Search);
        if (pattern is null)
        {
            return await Order(products, sort)
                .Select(ToListItem)
                .ToPagedResultAsync(page, cancellationToken)
                .ConfigureAwait(false);
        }

        // Busca por trecho (contém) em SKU ou nome: varre a tabela (~25 ms nos 100 mil produtos). Nem OR nem IN (... UNION ...) servem: com ORDER BY + LIMIT o
        // MySQL percorre o índice da ordenação lendo linha a linha e chega a 200-600 ms quando há poucos
        // casamentos. O caminho estável é um UNION estreito (id + colunas de ordenação), ordenado e
        // paginado dentro da tabela derivada, e só então a junção com o produto. Medições em
        // docs/modules/products.md.
        var keys = products
            .Where(p => EF.Functions.Like(p.Name, pattern, SearchPattern.EscapeCharacter))
            .Select(ToSearchKey)
            .Union(products
                .Where(p => EF.Functions.Like(p.Sku, pattern, SearchPattern.EscapeCharacter))
                .Select(ToSearchKey));

        var total = await keys.CountAsync(cancellationToken).ConfigureAwait(false);
        var pageKeys = OrderKeys(keys, sort).Skip(page.Skip).Take(page.PageSize);
        var pageProducts = from key in pageKeys
                           join product in db.Products.AsNoTracking() on key.Id equals product.Id
                           select product;

        var items = await Order(pageProducts, sort)
            .Select(ToListItem)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<ProductListItemDto>(items, page.Page, page.PageSize, total);
    }

    public async Task<ProductDto> GetAsync(long id, CancellationToken cancellationToken)
    {
        var product = await db.Products.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new ProductDto(
                p.Id,
                p.Code,
                p.Sku,
                p.Name,
                p.Description,
                p.CategoryId,
                p.Category!.Name,
                p.UnitId,
                p.Unit!.Code,
                p.Unit!.Name,
                p.CostPrice,
                p.SalePrice,
                p.MinStock,
                p.Active,
                p.Version,
                p.CreatedAt,
                p.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return product ?? throw NotFound();
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var data = new ProductData(
            request.Sku,
            request.Name,
            request.Description,
            request.CategoryId,
            request.UnitId,
            request.CostPrice,
            request.SalePrice,
            request.MinStock);

        var product = Product.Create(data);
        var categoryName = await EnsureReferencesExistAsync(product, cancellationToken).ConfigureAwait(false);
        if (!product.HasPendingSku)
        {
            await EnsureSkuIsFreeAsync(product.Sku, null, cancellationToken).ConfigureAwait(false);
        }

        await InsertWithNextCodeAsync(product, categoryName, cancellationToken).ConfigureAwait(false);
        return await GetAsync(product.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Cria uma cópia do produto: mesmos dados, nome com "(cópia)", SKU novo e o próximo código.</summary>
    public async Task<ProductDto> DuplicateAsync(long id, CancellationToken cancellationToken)
    {
        var source = await GetAsync(id, cancellationToken).ConfigureAwait(false);
        const string suffix = " (cópia)";
        var name = source.Name.Length + suffix.Length > Product.NameMaxLength
            ? source.Name[..(Product.NameMaxLength - suffix.Length)]
            : source.Name;
        return await CreateAsync(
            new CreateProductRequest(null, name + suffix, source.Description, source.CategoryId, source.UnitId, source.CostPrice, source.SalePrice, source.MinStock),
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Exclui um produto que nunca teve venda nem movimentação de estoque (por exemplo, um cadastro feito
    /// por engano). Os demais continuam existindo e podem ser inativados. O saldo zerado vai junto.
    /// </summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var product = await FindTrackedAsync(id, cancellationToken).ConfigureAwait(false);
        var used = await db.StockMovements.AnyAsync(m => m.ProductId == id, cancellationToken).ConfigureAwait(false)
            || await db.Sales.SelectMany(sale => sale.Items).AnyAsync(item => item.ProductId == id, cancellationToken).ConfigureAwait(false);
        if (used)
        {
            throw new ConflictException(
                ErrorCodes.ProductInUse,
                "O produto tem vendas ou movimentações de estoque e não pode ser excluído. Inative-o.");
        }

        var balances = await db.StockBalances.Where(b => b.ProductId == id).ToListAsync(cancellationToken).ConfigureAwait(false);
        db.StockBalances.RemoveRange(balances);
        db.Products.Remove(product);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ProductDto> UpdateAsync(long id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var product = await FindTrackedAsync(id, cancellationToken).ConfigureAwait(false);
        if (product.Version != request.Version)
        {
            throw ConflictException.StaleVersion();
        }

        product.Update(new ProductData(
            request.Sku,
            request.Name,
            request.Description,
            request.CategoryId,
            request.UnitId,
            request.CostPrice,
            request.SalePrice,
            request.MinStock));
        var categoryName = await EnsureReferencesExistAsync(product, cancellationToken).ConfigureAwait(false);
        if (product.HasPendingSku)
        {
            await AssignGeneratedSkuAsync(product, categoryName, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await EnsureSkuIsFreeAsync(product.Sku, product.Id, cancellationToken).ConfigureAwait(false);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return await GetAsync(product.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Ativa ou inativa. Produto nunca é excluído (ver docs/modules/products.md).</summary>
    public async Task<ProductDto> SetActiveAsync(long id, bool active, CancellationToken cancellationToken)
    {
        var product = await FindTrackedAsync(id, cancellationToken).ConfigureAwait(false);
        if (product.Active != active)
        {
            if (active)
            {
                product.Activate();
            }
            else
            {
                product.Deactivate();
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return await GetAsync(product.Id, cancellationToken).ConfigureAwait(false);
    }

    private Expression<Func<Product, ProductListItemDto>> ToListItem => p => new ProductListItemDto(
        p.Id,
        p.Code,
        p.Sku,
        p.Name,
        p.Description,
        p.CategoryId,
        p.Category!.Name,
        p.UnitId,
        p.Unit!.Code,
        p.CostPrice,
        p.SalePrice,
        p.MinStock,
        db.StockBalances.Where(b => b.ProductId == p.Id).Select(b => (decimal?)b.Quantity).FirstOrDefault() ?? 0,
        p.Active,
        p.UpdatedAt);

    private static readonly Expression<Func<Product, ProductSearchKey>> ToSearchKey = p => new ProductSearchKey
    {
        Id = p.Id,
        Code = p.Code,
        Name = p.Name,
        Sku = p.Sku,
        SalePrice = p.SalePrice,
        CreatedAt = p.CreatedAt,
    };

    private static IQueryable<Product> Order(IQueryable<Product> products, SortRequest sort) =>
        (sort.Field, sort.Descending) switch
        {
            ("name", false) => products.OrderBy(p => p.Name).ThenBy(p => p.Id),
            ("name", true) => products.OrderByDescending(p => p.Name).ThenByDescending(p => p.Id),
            ("sku", false) => products.OrderBy(p => p.Sku),
            ("sku", true) => products.OrderByDescending(p => p.Sku),
            ("salePrice", false) => products.OrderBy(p => p.SalePrice).ThenBy(p => p.Id),
            ("salePrice", true) => products.OrderByDescending(p => p.SalePrice).ThenByDescending(p => p.Id),
            ("createdAt", false) => products.OrderBy(p => p.CreatedAt).ThenBy(p => p.Id),
            ("createdAt", true) => products.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id),
            (_, true) => products.OrderByDescending(p => p.Code),
            _ => products.OrderBy(p => p.Code),
        };

    /// <summary>Mesma ordenação de <see cref="Order"/>, aplicada às chaves da busca.</summary>
    private static IQueryable<ProductSearchKey> OrderKeys(IQueryable<ProductSearchKey> keys, SortRequest sort) =>
        (sort.Field, sort.Descending) switch
        {
            ("name", false) => keys.OrderBy(k => k.Name).ThenBy(k => k.Id),
            ("name", true) => keys.OrderByDescending(k => k.Name).ThenByDescending(k => k.Id),
            ("sku", false) => keys.OrderBy(k => k.Sku),
            ("sku", true) => keys.OrderByDescending(k => k.Sku),
            ("salePrice", false) => keys.OrderBy(k => k.SalePrice).ThenBy(k => k.Id),
            ("salePrice", true) => keys.OrderByDescending(k => k.SalePrice).ThenByDescending(k => k.Id),
            ("createdAt", false) => keys.OrderBy(k => k.CreatedAt).ThenBy(k => k.Id),
            ("createdAt", true) => keys.OrderByDescending(k => k.CreatedAt).ThenByDescending(k => k.Id),
            (_, true) => keys.OrderByDescending(k => k.Code),
            _ => keys.OrderBy(k => k.Code),
        };

    /// <summary>
    /// Pega o próximo código e grava o produto, tudo na mesma transação. O UPDATE atômico da sequência trava
    /// a linha até o commit: criações simultâneas esperam a vez, nunca repetem o código e, se a criação
    /// falhar, o número volta com o rollback (sem lacunas).
    /// </summary>
    private async Task InsertWithNextCodeAsync(Product product, string? categoryName, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        product.AssignCode(await db.NextSequenceNumberAsync(DocumentSequence.Product, cancellationToken).ConfigureAwait(false));

        // SKU gerado depende do id: grava com o provisório e troca na mesma transação.
        db.Products.Add(product);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        // Todo produto nasce com saldo zero (StockBalance), para a movimentação só atualizar.
        db.StockBalances.Add(StockBalance.Empty(product.Id));
        if (product.HasPendingSku)
        {
            await AssignGeneratedSkuAsync(product, categoryName, cancellationToken).ConfigureAwait(false);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<Product> FindTrackedAsync(long id, CancellationToken cancellationToken) =>
        await db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken).ConfigureAwait(false)
        ?? throw NotFound();

    /// <summary>
    /// Valida categoria e unidade, quando informadas, e devolve o nome da categoria (prefixo do SKU
    /// gerado; nulo sem categoria).
    /// </summary>
    private async Task<string?> EnsureReferencesExistAsync(Product product, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();
        string? categoryName = null;
        if (product.CategoryId is { } categoryId)
        {
            categoryName = await db.ProductCategories
                .Where(c => c.Id == categoryId)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            if (categoryName is null)
            {
                errors.Add("categoryId", "Categoria não encontrada.");
            }
        }

        if (product.UnitId is { } unitId
            && !await db.Units.AnyAsync(u => u.Id == unitId, cancellationToken).ConfigureAwait(false))
        {
            errors.Add("unitId", "Unidade não encontrada.");
        }

        errors.ThrowIfAny();
        return categoryName;
    }

    /// <summary>Gera o SKU; se alguém já digitou o mesmo código, tenta com sufixo (-2, -3...).</summary>
    private async Task AssignGeneratedSkuAsync(Product product, string? categoryName, CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            var sku = Product.GenerateSku(categoryName, product.Id, attempt);
            var taken = await db.Products
                .AnyAsync(p => p.Sku == sku && p.Id != product.Id, cancellationToken)
                .ConfigureAwait(false);
            if (!taken)
            {
                product.AssignGeneratedSku(categoryName, attempt);
                return;
            }
        }
    }

    private async Task EnsureSkuIsFreeAsync(string sku, long? exceptId, CancellationToken cancellationToken)
    {
        var taken = await db.Products
            .AnyAsync(p => p.Sku == sku && (exceptId == null || p.Id != exceptId), cancellationToken)
            .ConfigureAwait(false);
        if (taken)
        {
            throw new ConflictException(ErrorCodes.ProductSkuDuplicate, $"Já existe um produto com o SKU {sku}.");
        }
    }

    private static NotFoundException NotFound() => new(ErrorCodes.ProductNotFound, "Produto não encontrado.");
}

/// <summary>Projeção estreita usada no UNION da busca: só o id e as colunas de ordenação.</summary>
internal sealed class ProductSearchKey
{
    public long Id { get; init; }

    public long Code { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Sku { get; init; } = string.Empty;

    public decimal SalePrice { get; init; }

    public DateTime CreatedAt { get; init; }
}
