using System.Diagnostics;
using System.Globalization;
using System.Text;
using Erp.Application.Abstractions;
using Erp.Application.Common;
using Erp.Application.Settings;
using Erp.Domain.Common;
using Erp.Domain.Stock;
using Microsoft.EntityFrameworkCore;

namespace Erp.Application.Stock;

/// <summary>
/// Saldo e movimentações de estoque. Toda movimentação grava o movimento e atualiza o saldo na mesma
/// transação; se outra gravação mudou o saldo no meio (versão), refaz a operação do zero.
/// </summary>
public sealed class StockService(IErpDbContext db, SettingsReader settings, ICurrentUser currentUser, TimeProvider clock)
{
    public static readonly IReadOnlyCollection<string> BalanceSortFields = ["name", "sku", "quantity"];

    /// <summary>Tentativas quando a versão do saldo muda entre a leitura e a gravação.</summary>
    private const int MaxAttempts = 3;

    /// <summary>Tolerância para relógios levemente adiantados ao informar a data do movimento.</summary>
    private static readonly TimeSpan FutureTolerance = TimeSpan.FromMinutes(5);

    public async Task<PagedResult<StockBalanceDto>> ListBalancesAsync(StockBalanceListQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var page = PageRequest.From(query.Page, query.PageSize);
        var sort = SortRequest.Parse(query.Sort, "name", BalanceSortFields);

        var products = db.Products.AsNoTracking();
        if (query.CategoryId is { } categoryId)
        {
            products = products.Where(p => p.CategoryId == categoryId);
        }

        var pattern = SearchPattern.Prefix(query.Search);
        if (pattern is not null)
        {
            products = products.Where(p => EF.Functions.Like(p.Name, pattern, SearchPattern.EscapeCharacter)
                || EF.Functions.Like(p.Sku, pattern, SearchPattern.EscapeCharacter));
        }

        // Produto sem saldo ainda (nunca movimentado) conta como zero.
        var rows = from product in products
                   join balance in db.StockBalances.AsNoTracking() on product.Id equals balance.ProductId into balances
                   from balance in balances.DefaultIfEmpty()
                   select new { Product = product, Quantity = balance == null ? 0 : balance.Quantity };

        if (query.LowStock is { } lowStock)
        {
            rows = lowStock
                ? rows.Where(r => r.Quantity < r.Product.MinStock)
                : rows.Where(r => r.Quantity >= r.Product.MinStock);
        }

        rows = (sort.Field, sort.Descending) switch
        {
            ("sku", false) => rows.OrderBy(r => r.Product.Sku),
            ("sku", true) => rows.OrderByDescending(r => r.Product.Sku),
            ("quantity", false) => rows.OrderBy(r => r.Quantity).ThenBy(r => r.Product.Id),
            ("quantity", true) => rows.OrderByDescending(r => r.Quantity).ThenByDescending(r => r.Product.Id),
            (_, true) => rows.OrderByDescending(r => r.Product.Name).ThenByDescending(r => r.Product.Id),
            _ => rows.OrderBy(r => r.Product.Name).ThenBy(r => r.Product.Id),
        };

        return await rows
            .Select(r => new StockBalanceDto(
                r.Product.Id,
                r.Product.Sku,
                r.Product.Name,
                r.Product.Category!.Name,
                r.Product.Unit!.Code,
                r.Quantity,
                r.Product.MinStock,
                r.Quantity < r.Product.MinStock,
                r.Product.Active))
            .ToPagedResultAsync(page, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Histórico do mais recente para o mais antigo, paginado por chave (data, id): cada página custa o
    /// mesmo, seja a primeira ou a milésima, mesmo com milhões de movimentações.
    /// </summary>
    public async Task<KeysetPage<StockMovementDto>> ListMovementsAsync(StockMovementQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        var pageSize = PageRequest.From(1, query.PageSize).PageSize;
        var cursor = MovementCursor.Parse(query.Cursor);

        var movements = db.StockMovements.AsNoTracking();
        if (query.ProductId is { } productId)
        {
            movements = movements.Where(m => m.ProductId == productId);
        }

        if (query.Type is { } type)
        {
            movements = movements.Where(m => m.Type == type);
        }

        if (query.From is { } from)
        {
            movements = movements.Where(m => m.OccurredAt >= from);
        }

        if (query.To is { } to)
        {
            movements = movements.Where(m => m.OccurredAt < to);
        }

        if (cursor is { } after)
        {
            movements = movements.Where(m => m.OccurredAt < after.OccurredAt
                || (m.OccurredAt == after.OccurredAt && m.Id < after.Id));
        }

        var items = await Project(movements
                .OrderByDescending(m => m.OccurredAt)
                .ThenByDescending(m => m.Id)
                .Take(pageSize + 1))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        string? next = null;
        if (items.Count > pageSize)
        {
            items.RemoveAt(pageSize);
            next = new MovementCursor(items[^1].OccurredAt, items[^1].Id).ToString();
        }

        return new KeysetPage<StockMovementDto>(items, next);
    }

    public async Task<StockMovementDto> GetMovementAsync(long id, CancellationToken cancellationToken) =>
        await Project(db.StockMovements.AsNoTracking().Where(m => m.Id == id))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
        ?? throw MovementNotFound();

    /// <summary>Lançamento manual de entrada, saída ou ajuste de inventário.</summary>
    public async Task<StockMovementDto> RecordAsync(CreateStockMovementRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var occurredAt = ResolveOccurredAt(request.OccurredAt);
        var userId = currentUser.Id;

        var movement = await ApplyAsync(
            request.ProductId,
            balance => request.Type switch
            {
                StockMovementType.Entry => StockMovement.Entry(request.ProductId, request.Quantity, request.UnitCost, occurredAt, userId, request.Note),
                StockMovementType.Exit => StockMovement.Exit(request.ProductId, request.Quantity, request.UnitCost, occurredAt, userId, request.Note),
                StockMovementType.Adjustment => StockMovement.Adjustment(request.ProductId, request.Quantity, balance.Quantity, occurredAt, userId, request.Note),
                _ => InvalidManualType(),
            },
            cancellationToken).ConfigureAwait(false);

        return await GetMovementAsync(movement.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Estorna um movimento uma única vez: grava a quantidade oposta e devolve o estorno.</summary>
    public async Task<StockMovementDto> ReverseAsync(long id, ReverseStockMovementRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var original = await db.StockMovements.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken).ConfigureAwait(false)
            ?? throw MovementNotFound();
        var userId = currentUser.Id;

        var reversal = await ApplyAsync(
            original.ProductId,
            _ => StockMovement.Reverse(original, clock.GetUtcNow().UtcDateTime, userId, request.Note),
            cancellationToken,
            ensureBeforeSave: async () =>
            {
                // Dentro da transação, para dois estornos simultâneos não passarem juntos.
                if (await IsReversedAsync(original.Id, cancellationToken).ConfigureAwait(false))
                {
                    throw new ConflictException(ErrorCodes.StockMovementAlreadyReversed, "Este movimento já foi estornado.");
                }
            }).ConfigureAwait(false);

        return await GetMovementAsync(reversal.Id, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Grava um movimento e atualiza o saldo numa transação. O movimento é montado a partir do saldo
    /// atual (o ajuste depende dele); conflito de versão refaz tudo, até <see cref="MaxAttempts"/> vezes.
    /// </summary>
    private async Task<StockMovement> ApplyAsync(
        long productId,
        Func<StockBalance, StockMovement> build,
        CancellationToken cancellationToken,
        Func<Task>? ensureBeforeSave = null)
    {
        await EnsureProductExistsAsync(productId, cancellationToken).ConfigureAwait(false);
        var allowNegative = await settings.StockAllowNegativeAsync(cancellationToken).ConfigureAwait(false);

        for (var attempt = 1; ; attempt++)
        {
            var transaction = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false))
            {
                try
                {
                    var balance = await db.StockBalances.FirstOrDefaultAsync(b => b.ProductId == productId, cancellationToken).ConfigureAwait(false);
                    if (balance is null)
                    {
                        balance = StockBalance.Empty(productId);
                        db.StockBalances.Add(balance);
                    }

                    var movement = build(balance);
                    if (ensureBeforeSave is not null)
                    {
                        await ensureBeforeSave().ConfigureAwait(false);
                    }

                    balance.Apply(movement, allowNegative);
                    db.StockMovements.Add(movement);
                    await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return movement;
                }
                catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
                {
                    // Outra movimentação do mesmo produto gravou antes: descarta e relê o saldo.
                    db.ChangeTracker.Clear();
                }
                catch
                {
                    db.ChangeTracker.Clear();
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Aplica um movimento dentro de uma transação aberta por outro caso de uso (confirmar ou cancelar
    /// venda): atualiza o saldo e adiciona o movimento, sem gravar. Quem chama grava, faz commit e trata o
    /// conflito de versão do saldo refazendo a operação inteira.
    /// </summary>
    public async Task ApplyWithinTransactionAsync(StockMovement movement, bool allowNegative, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(movement);
        var balance = db.StockBalances.Local.FirstOrDefault(b => b.ProductId == movement.ProductId)
            ?? await db.StockBalances.FirstOrDefaultAsync(b => b.ProductId == movement.ProductId, cancellationToken).ConfigureAwait(false);
        if (balance is null)
        {
            balance = StockBalance.Empty(movement.ProductId);
            db.StockBalances.Add(balance);
        }

        balance.Apply(movement, allowNegative);
        db.StockMovements.Add(movement);
    }

    private Task<bool> IsReversedAsync(long movementId, CancellationToken cancellationToken) =>
        db.StockMovements.AnyAsync(
            m => m.ReferenceType == StockMovement.MovementReference && m.ReferenceId == movementId,
            cancellationToken);

    private async Task EnsureProductExistsAsync(long productId, CancellationToken cancellationToken)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId, cancellationToken).ConfigureAwait(false))
        {
            new ValidationErrors().Add("productId", "Produto não encontrado.").ThrowIfAny();
        }
    }

    private DateTime ResolveOccurredAt(DateTime? occurredAt)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        if (occurredAt is not { } value)
        {
            return now;
        }

        var utc = value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value.ToUniversalTime(), DateTimeKind.Utc);
        if (utc > now + FutureTolerance)
        {
            new ValidationErrors().Add("occurredAt", "A data do movimento não pode ser no futuro.").ThrowIfAny();
        }

        return utc;
    }

    private IQueryable<StockMovementDto> Project(IQueryable<StockMovement> movements) =>
        from m in movements
        join product in db.Products on m.ProductId equals product.Id
        select new StockMovementDto(
            m.Id,
            m.ProductId,
            product.Sku,
            product.Name,
            product.Unit!.Code,
            m.Type,
            m.Quantity,
            m.UnitCost,
            m.OccurredAt,
            m.ReferenceType,
            m.ReferenceId,
            m.UserId,
            db.UserNames.Where(u => u.Id == m.UserId).Select(u => u.FullName).FirstOrDefault(),
            m.Note,
            db.StockMovements
                .Where(r => r.ReferenceType == StockMovement.MovementReference && r.ReferenceId == m.Id)
                .Select(r => (long?)r.Id)
                .FirstOrDefault(),
            m.CreatedAt);

    private static StockMovement InvalidManualType()
    {
        new ValidationErrors()
            .Add("type", "Use entrada, saída ou ajuste. Estorno é feito a partir do movimento original.")
            .ThrowIfAny();
        throw new UnreachableException();
    }

    private static NotFoundException MovementNotFound() =>
        new(ErrorCodes.StockMovementNotFound, "Movimentação não encontrada.");

    /// <summary>Posição na paginação por chave: data e id do último item, em base64url.</summary>
    private readonly record struct MovementCursor(DateTime OccurredAt, long Id)
    {
        public override string ToString()
        {
            var raw = string.Create(CultureInfo.InvariantCulture, $"{OccurredAt.Ticks}:{Id}");
            return Convert.ToBase64String(Encoding.ASCII.GetBytes(raw)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        public static MovementCursor? Parse(string? cursor)
        {
            if (string.IsNullOrWhiteSpace(cursor))
            {
                return null;
            }

            try
            {
                var base64 = cursor.Replace('-', '+').Replace('_', '/');
                base64 = base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');
                var parts = Encoding.ASCII.GetString(Convert.FromBase64String(base64)).Split(':');
                return new MovementCursor(
                    new DateTime(long.Parse(parts[0], CultureInfo.InvariantCulture), DateTimeKind.Utc),
                    long.Parse(parts[1], CultureInfo.InvariantCulture));
            }
            catch (Exception exception) when (exception is FormatException or IndexOutOfRangeException or OverflowException or ArgumentOutOfRangeException)
            {
                new ValidationErrors().Add("cursor", "Cursor inválido; recomece pela primeira página.").ThrowIfAny();
                return null;
            }
        }
    }
}
