using System.Net;
using System.Net.Http.Json;
using Erp.Application.Common;
using Erp.Application.Finance;
using Erp.Domain.Access;
using Erp.Domain.Finance;
using Erp.IntegrationTests.Infrastructure;
using static Erp.IntegrationTests.Infrastructure.ApiClient;

namespace Erp.IntegrationTests.Finance;

/// <summary>
/// Contas a receber e a pagar. Todos os testes de lançamento ficam nesta classe (em sequência), porque o
/// resumo soma todos os lançamentos do tipo.
/// </summary>
public sealed class FinanceEndpointsTests(MySqlApiFixture fixture)
{
    private static readonly DateOnly LongAgo = new(2020, 1, 10);
    private readonly HttpClient _client = fixture.ClientFor(Roles.Operator);

    /// <summary>Critério de saída da Fase 5: o ciclo aberto → pago → estornado (volta a aberto).</summary>
    [Fact]
    public async Task Cycle_OpenPaidReversed()
    {
        var category = await CategoryAsync(FinancialCategoryKind.Expense);
        var partner = await _client.CreatePartnerAsync(Erp.Domain.Partners.PartnerType.Supplier);
        var entry = Assert.Single(await CreateAsync(FinancialEntryKind.Payable, category.Id, 250m, LongAgo, partnerId: partner.Id));
        Assert.Equal((FinancialEntryStatus.Open, true, partner.Name), (entry.Status, entry.Overdue, entry.PartnerName));
        Assert.True(entry.DaysOverdue > 2000);

        var paid = await PostAsync<FinancialEntryDto>($"{entry.Id}/pay", new PayFinancialEntryRequest(new DateOnly(2020, 1, 12), 255.30m, entry.Version));
        Assert.Equal((FinancialEntryStatus.Paid, false, 0, 255.30m), (paid.Status, paid.Overdue, paid.DaysOverdue, paid.PaidAmount!.Value));

        using (var edit = await _client.PutAsJsonAsync(
            new Uri($"/api/v1/financial-entries/{entry.Id}", UriKind.Relative),
            new UpdateFinancialEntryRequest("Outro", category.Id, null, 1m, LongAgo, paid.Version),
            Json,
            Ct))
        {
            Assert.Equal(ErrorCodes.InvalidState, (await edit.ReadProblemAsync(HttpStatusCode.Conflict)).Code());
        }

        var reversed = await PostAsync<FinancialEntryDto>($"{entry.Id}/reverse-payment", new ReversePaymentRequest("pago em duplicidade", paid.Version));
        Assert.Equal((FinancialEntryStatus.Open, true), (reversed.Status, reversed.Overdue));
        Assert.Null(reversed.PaidAt);
        Assert.Equal("pago em duplicidade", reversed.ReversalReason);
        Assert.NotNull(reversed.ReversedAt);

        var cancelled = await PostAsync<FinancialEntryDto>($"{entry.Id}/cancel", new CancelFinancialEntryRequest(reversed.Version));
        Assert.Equal(FinancialEntryStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task Installments_SumTheTotal_OneRowEach()
    {
        var category = await CategoryAsync(FinancialCategoryKind.Revenue);

        var entries = await CreateAsync(FinancialEntryKind.Receivable, category.Id, 1000m, new DateOnly(2030, 1, 15), installments: 3);

        Assert.Equal(3, entries.Count);
        Assert.Equal(1000m, entries.Sum(e => e.Amount));
        Assert.Equal([1, 2, 3], entries.Select(e => e.InstallmentNo));
        Assert.All(entries, e => Assert.Equal(3, e.InstallmentTotal));
    }

    [Fact]
    public async Task Create_WithCategoryOfTheOtherKind_Returns400()
    {
        var revenue = await CategoryAsync(FinancialCategoryKind.Revenue);

        using var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1/financial-entries", UriKind.Relative),
            new CreateFinancialEntryRequest(FinancialEntryKind.Payable, "Luz", revenue.Id, null, 10m, LongAgo, 1),
            Json,
            Ct);

        var problem = await response.ReadProblemAsync(HttpStatusCode.BadRequest);
        Assert.Equal(["categoryId"], problem.ErrorFields());
    }

    [Fact]
    public async Task List_FiltersOverdueAndDuePeriod()
    {
        var category = await CategoryAsync(FinancialCategoryKind.Expense);
        var tag = Unique("Filtro ");
        await CreateAsync(FinancialEntryKind.Payable, category.Id, 10m, LongAgo, description: $"{tag} vencido");
        await CreateAsync(FinancialEntryKind.Payable, category.Id, 10m, new DateOnly(2031, 5, 10), description: $"{tag} futuro");

        var overdue = await ListAsync($"kind=Payable&status=Overdue&search={Uri.EscapeDataString(tag)}");
        var period = await ListAsync($"kind=Payable&dueFrom=2031-05-01&dueTo=2031-05-31&search={Uri.EscapeDataString(tag)}");
        var receivables = await ListAsync($"kind=Receivable&search={Uri.EscapeDataString(tag)}");

        Assert.Equal([$"{tag} vencido"], overdue.Items.Select(e => e.Description));
        Assert.Equal([$"{tag} futuro"], period.Items.Select(e => e.Description));
        Assert.Empty(receivables.Items);
    }

    [Fact]
    public async Task Summary_AddsOpenOverdueDueSoonAndPaidThisMonth()
    {
        var category = await CategoryAsync(FinancialCategoryKind.Revenue);
        var before = await SummaryAsync(FinancialEntryKind.Receivable);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-3));

        await CreateAsync(FinancialEntryKind.Receivable, category.Id, 100m, LongAgo);
        await CreateAsync(FinancialEntryKind.Receivable, category.Id, 40m, today.AddDays(3));
        var toPay = Assert.Single(await CreateAsync(FinancialEntryKind.Receivable, category.Id, 60m, today.AddDays(30)));
        await PostAsync<FinancialEntryDto>($"{toPay.Id}/pay", new PayFinancialEntryRequest(null, null, toPay.Version));

        var after = await SummaryAsync(FinancialEntryKind.Receivable);

        Assert.Equal(140m, after.OpenAmount - before.OpenAmount);
        Assert.Equal(100m, after.OverdueAmount - before.OverdueAmount);
        Assert.Equal(40m, after.DueNext7DaysAmount - before.DueNext7DaysAmount);
        Assert.Equal(60m, after.PaidThisMonthAmount - before.PaidThisMonthAmount);
    }

    [Fact]
    public async Task Pay_WithStaleVersion_Returns409()
    {
        var category = await CategoryAsync(FinancialCategoryKind.Expense);
        var entry = Assert.Single(await CreateAsync(FinancialEntryKind.Payable, category.Id, 10m, LongAgo));

        using var response = await _client.PostAsJsonAsync(
            new Uri($"/api/v1/financial-entries/{entry.Id}/pay", UriKind.Relative),
            new PayFinancialEntryRequest(null, null, entry.Version + 1),
            Json,
            Ct);

        Assert.Equal(ErrorCodes.ConcurrencyConflict, (await response.ReadProblemAsync(HttpStatusCode.Conflict)).Code());
    }

    [Fact]
    public async Task Categories_UniquePerKind_AndNotDeletedWhileInUse()
    {
        var name = Unique("Cat ");
        var expense = await CategoryAsync(FinancialCategoryKind.Expense, name);
        await CategoryAsync(FinancialCategoryKind.Revenue, name); // mesmo nome, outro tipo: ok

        using (var duplicate = await _client.PostAsJsonAsync(
            new Uri("/api/v1/financial-categories", UriKind.Relative),
            new SaveFinancialCategoryRequest(name, FinancialCategoryKind.Expense, null),
            Json,
            Ct))
        {
            Assert.Equal(ErrorCodes.FinancialCategoryNameDuplicate, (await duplicate.ReadProblemAsync(HttpStatusCode.Conflict)).Code());
        }

        await CreateAsync(FinancialEntryKind.Payable, expense.Id, 1m, LongAgo);
        using var delete = await _client.DeleteAsync(new Uri($"/api/v1/financial-categories/{expense.Id}", UriKind.Relative), Ct);
        Assert.Equal(ErrorCodes.FinancialCategoryInUse, (await delete.ReadProblemAsync(HttpStatusCode.Conflict)).Code());
    }

    [Fact]
    public async Task Viewer_CanReadButNotPay()
    {
        var category = await CategoryAsync(FinancialCategoryKind.Expense);
        var entry = Assert.Single(await CreateAsync(FinancialEntryKind.Payable, category.Id, 10m, LongAgo));
        var viewer = fixture.ClientFor(Roles.Viewer);

        using var read = await viewer.GetAsync(new Uri($"/api/v1/financial-entries/{entry.Id}", UriKind.Relative), Ct);
        using var pay = await viewer.PostAsJsonAsync(
            new Uri($"/api/v1/financial-entries/{entry.Id}/pay", UriKind.Relative), new PayFinancialEntryRequest(null, null, entry.Version), Json, Ct);

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, pay.StatusCode);
    }

    private async Task<FinancialCategoryDto> CategoryAsync(FinancialCategoryKind kind, string? name = null)
    {
        using var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1/financial-categories", UriKind.Relative),
            new SaveFinancialCategoryRequest(name ?? Unique("Categoria "), kind, null),
            Json,
            Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<FinancialCategoryDto>(Json, Ct))!;
    }

    private async Task<IReadOnlyList<FinancialEntryDto>> CreateAsync(
        FinancialEntryKind kind,
        long categoryId,
        decimal amount,
        DateOnly dueDate,
        int installments = 1,
        long? partnerId = null,
        string? description = null)
    {
        using var response = await _client.PostAsJsonAsync(
            new Uri("/api/v1/financial-entries", UriKind.Relative),
            new CreateFinancialEntryRequest(kind, description ?? Unique("Lançamento "), categoryId, partnerId, amount, dueDate, installments),
            Json,
            Ct);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<List<FinancialEntryDto>>(Json, Ct))!;
    }

    private async Task<T> PostAsync<T>(string path, object body)
    {
        using var response = await _client.PostAsJsonAsync(new Uri($"/api/v1/financial-entries/{path}", UriKind.Relative), body, Json, Ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>(Json, Ct))!;
    }

    private async Task<PagedResult<FinancialEntryDto>> ListAsync(string query) =>
        (await _client.GetFromJsonAsync<PagedResult<FinancialEntryDto>>(new Uri($"/api/v1/financial-entries?{query}", UriKind.Relative), Json, Ct))!;

    private async Task<FinancialSummaryDto> SummaryAsync(FinancialEntryKind kind) =>
        (await _client.GetFromJsonAsync<FinancialSummaryDto>(new Uri($"/api/v1/financial-entries/summary?kind={kind}", UriKind.Relative), Json, Ct))!;
}
