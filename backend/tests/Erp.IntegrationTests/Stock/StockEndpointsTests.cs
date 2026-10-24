using System.Net;
using System.Net.Http.Json;
using Erp.Application.Common;
using Erp.Application.Products;
using Erp.Application.Stock;
using Erp.Domain.Access;
using Erp.Domain.Settings;
using Erp.Domain.Stock;
using Erp.Infrastructure.Persistence;
using Erp.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Erp.IntegrationTests.Infrastructure.ApiClient;

namespace Erp.IntegrationTests.Stock;

/// <summary>
/// Movimentações e saldo. Todos os testes de estoque ficam nesta classe (rodam em sequência) porque um
/// deles liga a configuração global de saldo negativo.
/// </summary>
public sealed class StockEndpointsTests(MySqlApiFixture fixture)
{
    private readonly HttpClient _client = fixture.ClientFor(Roles.Operator);

    [Fact]
    public async Task NewProduct_StartsWithZeroBalance()
    {
        var product = await NewProductAsync();

        Assert.Equal(0m, await BalanceAsync(product.Id));
    }

    [Fact]
    public async Task EntryExitAndAdjustment_UpdateTheBalance()
    {
        var product = await NewProductAsync();

        var entry = await RecordAsync(product.Id, StockMovementType.Entry, 10m, unitCost: 2.5m);
        await RecordAsync(product.Id, StockMovementType.Exit, 3.5m);
        var adjustment = await RecordAsync(product.Id, StockMovementType.Adjustment, 5m, note: "inventário de outubro");

        Assert.Equal((10m, 2.5m), (entry.Quantity, entry.UnitCost));
        Assert.Equal(-1.5m, adjustment.Quantity);
        Assert.Equal(5m, await BalanceAsync(product.Id));
        Assert.Equal("Usuário Operator", entry.UserName);
    }

    [Fact]
    public async Task Exit_BeyondTheBalance_Returns400OnQuantity()
    {
        var product = await NewProductAsync();
        await RecordAsync(product.Id, StockMovementType.Entry, 2m);

        using var response = await PostMovementAsync(product.Id, StockMovementType.Exit, 3m);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["quantity"], problem.ErrorFields());
        Assert.Equal(2m, await BalanceAsync(product.Id));
    }

    [Fact]
    public async Task NegativeBalance_IsAllowedWhenTheSettingSaysSo()
    {
        var product = await NewProductAsync();
        await SetAllowNegativeAsync(true);
        try
        {
            await RecordAsync(product.Id, StockMovementType.Exit, 3m);

            Assert.Equal(-3m, await BalanceAsync(product.Id));
        }
        finally
        {
            await SetAllowNegativeAsync(false);
        }
    }

    [Fact]
    public async Task Reverse_WritesTheOppositeMovement_OnlyOnce()
    {
        var product = await NewProductAsync();
        var entry = await RecordAsync(product.Id, StockMovementType.Entry, 7m);

        using var reverse = await _client.PostAsJsonAsync(
            new Uri($"/api/v1/stock-movements/{entry.Id}/reverse", UriKind.Relative), new ReverseStockMovementRequest("lançado em dobro"), Json, Ct);
        Assert.Equal(HttpStatusCode.Created, reverse.StatusCode);
        var reversal = (await reverse.Content.ReadFromJsonAsync<StockMovementDto>(Json, Ct))!;

        using var again = await _client.PostAsJsonAsync(
            new Uri($"/api/v1/stock-movements/{entry.Id}/reverse", UriKind.Relative), new ReverseStockMovementRequest(null), Json, Ct);
        using var ofReversal = await _client.PostAsJsonAsync(
            new Uri($"/api/v1/stock-movements/{reversal.Id}/reverse", UriKind.Relative), new ReverseStockMovementRequest(null), Json, Ct);

        Assert.Equal((StockMovementType.Reversal, -7m, entry.Id), (reversal.Type, reversal.Quantity, reversal.ReferenceId!.Value));
        Assert.Equal(ErrorCodes.StockMovementAlreadyReversed, (await again.ReadProblemAsync(HttpStatusCode.Conflict)).Code());
        Assert.Equal(HttpStatusCode.BadRequest, ofReversal.StatusCode);
        Assert.Equal(0m, await BalanceAsync(product.Id));

        var original = await _client.GetFromJsonAsync<StockMovementDto>(new Uri($"/api/v1/stock-movements/{entry.Id}", UriKind.Relative), Json, Ct);
        Assert.Equal(reversal.Id, original!.ReversedById);
    }

    [Fact]
    public async Task History_PagesByKey_NewestFirst_WithoutGapsOrRepeats()
    {
        var product = await NewProductAsync();
        var created = new List<long>();
        for (var i = 1; i <= 5; i++)
        {
            created.Add((await RecordAsync(product.Id, StockMovementType.Entry, i)).Id);
        }

        var seen = new List<long>();
        string? cursor = null;
        do
        {
            var page = await ListMovementsAsync($"productId={product.Id}&pageSize=2" + (cursor is null ? "" : $"&cursor={cursor}"));
            Assert.True(page.Items.Count <= 2);
            seen.AddRange(page.Items.Select(m => m.Id));
            cursor = page.NextCursor;
        }
        while (cursor is not null);

        Assert.Equal(created.AsEnumerable().Reverse(), seen);
    }

    [Fact]
    public async Task History_InvalidCursor_Returns400()
    {
        using var response = await _client.GetAsync(new Uri("/api/v1/stock-movements?cursor=lixo", UriKind.Relative), Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["cursor"], problem.ErrorFields());
    }

    [Fact]
    public async Task ConcurrentExits_NeverTakeTheBalanceBelowZero()
    {
        var product = await NewProductAsync();
        await RecordAsync(product.Id, StockMovementType.Entry, 5m);

        // 12 saídas de 1 unidade ao mesmo tempo, de clientes diferentes, sobre um saldo de 5.
        var clients = new[] { _client, fixture.ClientFor(Roles.Admin) };
        var responses = await Task.WhenAll(Enumerable.Range(0, 12).Select(i =>
            clients[i % 2].PostAsJsonAsync(
                new Uri("/api/v1/stock-movements", UriKind.Relative),
                new CreateStockMovementRequest(product.Id, StockMovementType.Exit, 1m, null, null, null),
                Json,
                Ct)));

        var created = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
        foreach (var response in responses)
        {
            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.Conflict });
            response.Dispose();
        }

        var balance = await BalanceAsync(product.Id);
        Assert.True(balance >= 0, $"saldo negativo: {balance}");
        Assert.Equal(5m - created, balance);
    }

    /// <summary>Critério de saída da Fase 4: a soma das movimentações de cada produto é igual ao saldo.</summary>
    [Fact]
    public async Task Invariant_SumOfMovementsEqualsBalance_AfterMixedConcurrentOperations()
    {
        var products = new List<ProductDto>();
        for (var i = 0; i < 3; i++)
        {
            products.Add(await NewProductAsync());
        }

        var random = new Random(4);
        var operations = new List<Task>();
        foreach (var product in products)
        {
            await RecordAsync(product.Id, StockMovementType.Entry, 50m);
            for (var i = 0; i < 15; i++)
            {
                var type = random.Next(3) switch { 0 => StockMovementType.Entry, 1 => StockMovementType.Exit, _ => StockMovementType.Adjustment };
                var quantity = type == StockMovementType.Adjustment ? random.Next(0, 80) : random.Next(1, 20);
                operations.Add(SendIgnoringRejectionsAsync(product.Id, type, quantity));
            }
        }

        await Task.WhenAll(operations);

        // Estorna alguns movimentos depois, também em paralelo.
        var history = await ListMovementsAsync($"productId={products[0].Id}&pageSize=10");
        await Task.WhenAll(history.Items.Take(5).Select(m => _client.PostAsJsonAsync(
            new Uri($"/api/v1/stock-movements/{m.Id}/reverse", UriKind.Relative), new ReverseStockMovementRequest(null), Json, Ct)));

        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var ids = products.Select(p => p.Id).ToList();
        var sums = await db.StockMovements.Where(m => ids.Contains(m.ProductId))
            .GroupBy(m => m.ProductId)
            .Select(g => new { ProductId = g.Key, Sum = g.Sum(m => m.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Sum, Ct);
        var balances = await db.StockBalances.Where(b => ids.Contains(b.ProductId))
            .ToDictionaryAsync(b => b.ProductId, b => b.Quantity, Ct);

        foreach (var id in ids)
        {
            Assert.Equal(sums[id], balances[id]);
            Assert.True(balances[id] >= 0);
        }
    }

    [Fact]
    public async Task Viewer_CannotRecordMovements()
    {
        var product = await NewProductAsync();

        using var response = await fixture.ClientFor(Roles.Viewer).PostAsJsonAsync(
            new Uri("/api/v1/stock-movements", UriKind.Relative),
            new CreateStockMovementRequest(product.Id, StockMovementType.Entry, 1m, null, null, null),
            Json,
            Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Balances_AndProducts_ShowLowStock()
    {
        var category = await _client.CreateCategoryAsync();
        var unit = await _client.CreateUnitAsync();
        var low = await _client.CreateProductAsync(category.Id, unit.Id); // estoque mínimo 1, saldo 0
        var ok = await _client.CreateProductAsync(category.Id, unit.Id);
        await RecordAsync(ok.Id, StockMovementType.Entry, 3m);

        var balances = await _client.GetFromJsonAsync<PagedResult<StockBalanceDto>>(
            new Uri($"/api/v1/stock-balances?categoryId={category.Id}&lowStock=true", UriKind.Relative), Json, Ct);
        var products = await _client.GetFromJsonAsync<PagedResult<ProductListItemDto>>(
            new Uri($"/api/v1/products?categoryId={category.Id}&sort=name", UriKind.Relative), Json, Ct);

        Assert.Equal([low.Id], balances!.Items.Select(b => b.ProductId));
        Assert.True(balances.Items[0].LowStock);
        Assert.Equal(3m, products!.Items.Single(p => p.Id == ok.Id).StockQuantity);
    }

    private async Task<ProductDto> NewProductAsync()
    {
        var category = await _client.CreateCategoryAsync();
        var unit = await _client.CreateUnitAsync();
        return await _client.CreateProductAsync(category.Id, unit.Id);
    }

    private async Task<HttpResponseMessage> PostMovementAsync(long productId, StockMovementType type, decimal quantity, decimal? unitCost = null, string? note = null) =>
        await _client.PostAsJsonAsync(
            new Uri("/api/v1/stock-movements", UriKind.Relative),
            new CreateStockMovementRequest(productId, type, quantity, unitCost, null, note ?? (type == StockMovementType.Adjustment ? "contagem" : null)),
            Json,
            Ct);

    private async Task<StockMovementDto> RecordAsync(long productId, StockMovementType type, decimal quantity, decimal? unitCost = null, string? note = null)
    {
        using var response = await PostMovementAsync(productId, type, quantity, unitCost, note);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<StockMovementDto>(Json, Ct))!;
    }

    /// <summary>Operação concorrente: recusa de regra (saldo, ajuste sem diferença) faz parte do teste.</summary>
    private async Task SendIgnoringRejectionsAsync(long productId, StockMovementType type, decimal quantity)
    {
        using var response = await PostMovementAsync(productId, type, quantity);
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.Conflict });
    }

    private async Task<KeysetPage<StockMovementDto>> ListMovementsAsync(string query) =>
        (await _client.GetFromJsonAsync<KeysetPage<StockMovementDto>>(new Uri($"/api/v1/stock-movements?{query}", UriKind.Relative), Json, Ct))!;

    private async Task<decimal> BalanceAsync(long productId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        return await db.StockBalances.Where(b => b.ProductId == productId).Select(b => b.Quantity).SingleAsync(Ct);
    }

    private async Task SetAllowNegativeAsync(bool allow)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        var setting = await db.AppSettings.FirstOrDefaultAsync(s => s.Key == SettingKeys.StockAllowNegative, Ct);
        if (setting is null)
        {
            db.AppSettings.Add(AppSetting.Create(SettingKeys.StockAllowNegative, allow.ToString()));
        }
        else
        {
            setting.Change(allow.ToString());
        }

        await db.SaveChangesAsync(Ct);
    }
}
