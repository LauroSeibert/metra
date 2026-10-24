using System.Net;
using System.Net.Http.Json;
using Erp.Application.Common;
using Erp.Application.Products;
using Erp.Application.Sales;
using Erp.Application.Stock;
using Erp.Domain.Access;
using Erp.Domain.Finance;
using Erp.Domain.Partners;
using Erp.Domain.Sales;
using Erp.Domain.Stock;
using Erp.Infrastructure.Persistence;
using Erp.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Erp.IntegrationTests.Infrastructure.ApiClient;

namespace Erp.IntegrationTests.Sales;

/// <summary>Vendas integradas a estoque e financeiro.</summary>
public sealed class SaleEndpointsTests(MySqlApiFixture fixture)
{
    private readonly HttpClient _client = fixture.ClientFor(Roles.Operator);

    /// <summary>
    /// Critério de saída da Fase 6: confirmar baixa o estoque e gera as parcelas; cancelar desfaz os dois.
    /// </summary>
    [Fact]
    public async Task Chain_ConfirmLowersStockAndCreatesInstallments_CancelUndoesBoth()
    {
        var (customer, pen, notebook) = await ScenarioAsync(penStock: 20m, notebookStock: 5m);

        var draft = await CreateDraftAsync(customer.Id, PaymentMethod.CreditCard, 3, discount: 2m,
            new SaveSaleItemRequest(pen.Id, 4m, 0m),
            new SaveSaleItemRequest(notebook.Id, 1m, 1m));
        // Caneta 4 × 10 = 40; caderno 1 × 25 − 1 = 24; subtotal 64; total 62.
        Assert.Equal((SaleStatus.Draft, 64m, 62m), (draft.Status, draft.Subtotal, draft.Total));
        Assert.Equal(20m, await BalanceAsync(pen.Id));

        var confirmed = await PostAsync($"{draft.Id}/confirm", new ConfirmSaleRequest(draft.Version));
        Assert.Equal(SaleStatus.Confirmed, confirmed.Status);
        Assert.Equal((16m, 4m), (await BalanceAsync(pen.Id), await BalanceAsync(notebook.Id)));
        Assert.Equal(3, confirmed.Receivables.Count);
        Assert.Equal(62m, confirmed.Receivables.Sum(r => r.Amount));
        Assert.All(confirmed.Receivables, r => Assert.Equal(FinancialEntryStatus.Open, r.Status));
        Assert.Equal(draft.IssuedAt.AddMonths(1), confirmed.Receivables[0].DueDate);

        var cancelled = await PostAsync($"{draft.Id}/cancel", new CancelSaleRequest("cliente desistiu", confirmed.Version));
        Assert.Equal(SaleStatus.Cancelled, cancelled.Status);
        Assert.Equal((20m, 5m), (await BalanceAsync(pen.Id), await BalanceAsync(notebook.Id)));
        Assert.All(cancelled.Receivables, r => Assert.Equal(FinancialEntryStatus.Cancelled, r.Status));

        await AssertInvariantAsync(pen.Id, notebook.Id);
        var movements = await _client.GetFromJsonAsync<KeysetPage<StockMovementDto>>(
            new Uri($"/api/v1/stock-movements?productId={pen.Id}", UriKind.Relative), Json, Ct);
        Assert.Equal(
            [StockMovementType.SaleCancellation, StockMovementType.Sale, StockMovementType.Entry],
            movements!.Items.Select(m => m.Type));
    }

    [Fact]
    public async Task ImmediatePayment_CreatesOnePaidInstallment_AndCancelReversesIt()
    {
        var (customer, pen, _) = await ScenarioAsync(penStock: 10m);
        var draft = await CreateDraftAsync(customer.Id, PaymentMethod.Pix, 1, 0m, new SaveSaleItemRequest(pen.Id, 2m, 0m));

        var confirmed = await PostAsync($"{draft.Id}/confirm", new ConfirmSaleRequest(draft.Version));
        var installment = Assert.Single(confirmed.Receivables);
        Assert.Equal((FinancialEntryStatus.Paid, 20m), (installment.Status, installment.Amount));

        var cancelled = await PostAsync($"{draft.Id}/cancel", new CancelSaleRequest(null, confirmed.Version));
        Assert.Equal(FinancialEntryStatus.Cancelled, Assert.Single(cancelled.Receivables).Status);
    }

    [Fact]
    public async Task Confirm_WithoutStock_IsRejected_AndNothingChanges()
    {
        var (customer, pen, _) = await ScenarioAsync(penStock: 1m);
        var draft = await CreateDraftAsync(customer.Id, PaymentMethod.Cash, 1, 0m, new SaveSaleItemRequest(pen.Id, 3m, 0m));

        using var response = await _client.PostAsJsonAsync(new Uri($"/api/v1/sales/{draft.Id}/confirm", UriKind.Relative), new ConfirmSaleRequest(draft.Version), Json, Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["quantity"], problem.ErrorFields());
        var after = await GetAsync(draft.Id);
        Assert.Equal((SaleStatus.Draft, 1m), (after.Status, await BalanceAsync(pen.Id)));
        Assert.Empty(after.Receivables);
    }

    [Fact]
    public async Task Discount_AboveTheMaximum_Returns400()
    {
        var (customer, pen, _) = await ScenarioAsync(penStock: 10m);

        using var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1/sales", UriKind.Relative),
            Request(customer.Id, PaymentMethod.Cash, 1, 3m, new SaveSaleItemRequest(pen.Id, 2m, 0m)),
            Json,
            Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["discount"], problem.ErrorFields());
    }

    [Fact]
    public async Task OnlyCustomersAndActiveProducts_AreSold()
    {
        var (_, pen, _) = await ScenarioAsync(penStock: 10m);
        var supplier = await _client.CreatePartnerAsync(PartnerType.Supplier);
        using (var deactivate = await _client.PostAsync(new Uri($"/api/v1/products/{pen.Id}/deactivate", UriKind.Relative), null, Ct))
        {
            deactivate.EnsureSuccessStatusCode();
        }

        using var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1/sales", UriKind.Relative),
            Request(supplier.Id, PaymentMethod.Cash, 1, 0m, new SaveSaleItemRequest(pen.Id, 1m, 0m)),
            Json,
            Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["items", "partnerId"], problem.ErrorFields());
    }

    [Fact]
    public async Task Draft_IsEditable_ButConfirmedIsNot_AndPriceComesFromTheProduct()
    {
        var (customer, pen, notebook) = await ScenarioAsync(penStock: 10m, notebookStock: 10m);
        var draft = await CreateDraftAsync(customer.Id, PaymentMethod.Cash, 1, 0m, new SaveSaleItemRequest(pen.Id, 1m, 0m));

        using var update = await _client.PutAsJsonAsync(
            new Uri($"/api/v1/sales/{draft.Id}", UriKind.Relative),
            Request(customer.Id, PaymentMethod.BankSlip, 2, 0m, new SaveSaleItemRequest(notebook.Id, 2m, 0m)) with { Version = draft.Version },
            Json,
            Ct);
        update.EnsureSuccessStatusCode();
        var updated = (await update.Content.ReadFromJsonAsync<SaleDto>(Json, Ct))!;
        var item = Assert.Single(updated.Items);
        Assert.Equal((notebook.Id, 25m, 50m), (item.ProductId, item.UnitPrice, updated.Total));

        var confirmed = await PostAsync($"{draft.Id}/confirm", new ConfirmSaleRequest(updated.Version));
        using var edit = await _client.PutAsJsonAsync(
            new Uri($"/api/v1/sales/{draft.Id}", UriKind.Relative),
            Request(customer.Id, PaymentMethod.Cash, 1, 0m, new SaveSaleItemRequest(pen.Id, 1m, 0m)) with { Version = confirmed.Version },
            Json,
            Ct);
        Assert.Equal(ErrorCodes.InvalidState, (await edit.ReadProblemAsync(HttpStatusCode.Conflict)).Code());
    }

    [Fact]
    public async Task ConcurrentDrafts_GetConsecutiveNumbers_WithoutGapsOrRepeats()
    {
        var (customer, pen, _) = await ScenarioAsync(penStock: 1m);
        var clients = new[] { _client, fixture.ClientFor(Roles.Admin) };

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => clients[i % 2].PostAsJsonAsync(
            new Uri("/api/v1/sales", UriKind.Relative),
            Request(customer.Id, PaymentMethod.Cash, 1, 0m, new SaveSaleItemRequest(pen.Id, 1m, 0m)),
            Json,
            Ct)));

        var numbers = new List<int>();
        foreach (var response in responses)
        {
            Assert.True(response.StatusCode == HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
            numbers.Add((await response.Content.ReadFromJsonAsync<SaleDto>(Json, Ct))!.Number);
            response.Dispose();
        }

        numbers.Sort();
        Assert.Equal(8, numbers.Distinct().Count());
        Assert.Equal(Enumerable.Range(numbers[0], 8), numbers);
    }

    [Fact]
    public async Task List_FiltersByCustomerAndStatus()
    {
        var (customer, pen, _) = await ScenarioAsync(penStock: 10m);
        var draft = await CreateDraftAsync(customer.Id, PaymentMethod.Cash, 1, 0m, new SaveSaleItemRequest(pen.Id, 1m, 0m));
        await PostAsync($"{draft.Id}/confirm", new ConfirmSaleRequest(draft.Version));
        await CreateDraftAsync(customer.Id, PaymentMethod.Cash, 1, 0m, new SaveSaleItemRequest(pen.Id, 1m, 0m));

        var confirmed = await _client.GetFromJsonAsync<PagedResult<SaleListItemDto>>(
            new Uri($"/api/v1/sales?partnerId={customer.Id}&status=Confirmed", UriKind.Relative), Json, Ct);
        var all = await _client.GetFromJsonAsync<PagedResult<SaleListItemDto>>(
            new Uri($"/api/v1/sales?partnerId={customer.Id}", UriKind.Relative), Json, Ct);

        Assert.Equal([draft.Number], confirmed!.Items.Select(s => s.Number));
        Assert.Equal(2, all!.TotalCount);
        Assert.Equal(customer.Name, all.Items[0].PartnerName);
        Assert.True(all.Items[0].Number > all.Items[1].Number);
    }

    [Fact]
    public async Task Viewer_CannotSell()
    {
        var (customer, pen, _) = await ScenarioAsync(penStock: 10m);

        using var response = await fixture.ClientFor(Roles.Viewer).PostAsJsonAsync(
            new Uri("/api/v1/sales", UriKind.Relative),
            Request(customer.Id, PaymentMethod.Cash, 1, 0m, new SaveSaleItemRequest(pen.Id, 1m, 0m)),
            Json,
            Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>Cliente, caneta (R$ 10) e caderno (R$ 25) com o estoque pedido.</summary>
    private async Task<(Application.Partners.PartnerDto Customer, ProductDto Pen, ProductDto Notebook)> ScenarioAsync(decimal penStock, decimal notebookStock = 0m)
    {
        var customer = await _client.CreatePartnerAsync(PartnerType.Customer);
        var category = await _client.CreateCategoryAsync();
        var unit = await _client.CreateUnitAsync();
        var pen = await _client.CreateProductAsync(category.Id, unit.Id, name: "Caneta", salePrice: 10m);
        var notebook = await _client.CreateProductAsync(category.Id, unit.Id, name: "Caderno", salePrice: 25m);
        foreach (var (product, quantity) in new[] { (pen, penStock), (notebook, notebookStock) })
        {
            if (quantity > 0)
            {
                using var entry = await _client.PostAsJsonAsync(
                    new Uri("/api/v1/stock-movements", UriKind.Relative),
                    new CreateStockMovementRequest(product.Id, StockMovementType.Entry, quantity, null, null, null),
                    Json,
                    Ct);
                entry.EnsureSuccessStatusCode();
            }
        }

        return (customer, pen, notebook);
    }

    private static SaveSaleRequest Request(long partnerId, PaymentMethod method, int installments, decimal discount, params SaveSaleItemRequest[] items) =>
        new(partnerId, method, installments, discount, null, null, items, null);

    private async Task<SaleDto> CreateDraftAsync(long partnerId, PaymentMethod method, int installments, decimal discount, params SaveSaleItemRequest[] items)
    {
        using var response = await _client.PostAsJsonAsync(new Uri("/api/v1/sales", UriKind.Relative), Request(partnerId, method, installments, discount, items), Json, Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SaleDto>(Json, Ct))!;
    }

    private async Task<SaleDto> PostAsync(string path, object body)
    {
        using var response = await _client.PostAsJsonAsync(new Uri($"/api/v1/sales/{path}", UriKind.Relative), body, Json, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SaleDto>(Json, Ct))!;
    }

    private async Task<SaleDto> GetAsync(long id) =>
        (await _client.GetFromJsonAsync<SaleDto>(new Uri($"/api/v1/sales/{id}", UriKind.Relative), Json, Ct))!;

    private async Task<decimal> BalanceAsync(long productId)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        return await db.StockBalances.Where(b => b.ProductId == productId).Select(b => b.Quantity).SingleAsync(Ct);
    }

    private async Task AssertInvariantAsync(params long[] productIds)
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        foreach (var id in productIds)
        {
            var sum = await db.StockMovements.Where(m => m.ProductId == id).SumAsync(m => m.Quantity, Ct);
            Assert.Equal(sum, await BalanceAsync(id));
        }
    }
}
