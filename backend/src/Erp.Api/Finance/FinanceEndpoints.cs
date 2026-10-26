using Erp.Api.Access;
using Erp.Application.Common;
using Erp.Application.Finance;
using Erp.Domain.Finance;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Erp.Api.Finance;

/// <summary>Categorias financeiras e lançamentos (contas a receber e a pagar).</summary>
internal static class FinanceEndpoints
{
    public static RouteGroupBuilder MapFinanceEndpoints(this RouteGroupBuilder api)
    {
        var categories = api.MapGroup("/financial-categories").WithTags("FinancialCategories");
        categories.MapGet("/", ListCategoriesAsync).WithName("ListFinancialCategories").ProducesValidationProblem();
        categories.MapGet("/{id:long}", GetCategoryAsync).WithName("GetFinancialCategory").ProducesProblem(StatusCodes.Status404NotFound);
        categories.MapPost("/", CreateCategoryAsync)
            .WithName("CreateFinancialCategory")
            .RequireWrite()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapPut("/{id:long}", UpdateCategoryAsync)
            .WithName("UpdateFinancialCategory")
            .RequireWrite()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        categories.MapDelete("/{id:long}", DeleteCategoryAsync)
            .WithName("DeleteFinancialCategory")
            .RequireWrite()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        var entries = api.MapGroup("/financial-entries").WithTags("FinancialEntries");
        entries.MapGet("/", ListEntriesAsync)
            .WithName("ListFinancialEntries")
            .WithSummary("Contas a receber ou a pagar, com filtros de situação (inclusive vencido) e vencimento.")
            .ProducesValidationProblem();
        entries.MapGet("/summary", SummaryAsync)
            .WithName("GetFinancialSummary")
            .WithSummary("Totais em aberto, vencido, a vencer em 7 dias e pago no mês.");
        entries.MapGet("/{id:long}", GetEntryAsync).WithName("GetFinancialEntry").ProducesProblem(StatusCodes.Status404NotFound);
        entries.MapGet("/{id:long}/installments", ListInstallmentsAsync)
            .WithName("ListFinancialEntryInstallments")
            .WithSummary("Parcelas da mesma conta do lançamento, em ordem.")
            .ProducesProblem(StatusCodes.Status404NotFound);
        entries.MapPost("/", CreateEntryAsync)
            .WithName("CreateFinancialEntry")
            .WithSummary("Cria a conta já dividida em parcelas mensais; devolve as parcelas.")
            .RequireWrite()
            .ProducesValidationProblem();
        entries.MapPut("/{id:long}", UpdateEntryAsync)
            .WithName("UpdateFinancialEntry")
            .RequireWrite()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        entries.MapPost("/{id:long}/pay", PayAsync)
            .WithName("PayFinancialEntry")
            .WithSummary("Baixa: registra data e valor pago.")
            .RequireWrite()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        entries.MapPost("/{id:long}/reverse-payment", ReversePaymentAsync)
            .WithName("ReverseFinancialPayment")
            .WithSummary("Estorna a baixa: o lançamento volta a aberto.")
            .RequireWrite()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
        entries.MapPost("/{id:long}/cancel", CancelAsync)
            .WithName("CancelFinancialEntry")
            .RequireWrite()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return api;
    }

    private static async Task<Ok<PagedResult<FinancialCategoryDto>>> ListCategoriesAsync(
        FinancialCategoryService service,
        string? search,
        FinancialCategoryKind? kind,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListAsync(search, kind, page, pageSize, cancellationToken).ConfigureAwait(false));

    private static async Task<Ok<FinancialCategoryDto>> GetCategoryAsync(long id, FinancialCategoryService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAsync(id, cancellationToken).ConfigureAwait(false));

    private static async Task<Created<FinancialCategoryDto>> CreateCategoryAsync(
        SaveFinancialCategoryRequest request,
        FinancialCategoryService service,
        CancellationToken cancellationToken)
    {
        var category = await service.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return TypedResults.Created($"/api/v1/financial-categories/{category.Id}", category);
    }

    private static async Task<Ok<FinancialCategoryDto>> UpdateCategoryAsync(
        long id,
        SaveFinancialCategoryRequest request,
        FinancialCategoryService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false));

    private static async Task<NoContent> DeleteCategoryAsync(long id, FinancialCategoryService service, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return TypedResults.NoContent();
    }

    private static async Task<Ok<PagedResult<FinancialEntryDto>>> ListEntriesAsync(
        FinancialEntryService service,
        FinancialEntryKind kind,
        string? search,
        FinancialEntryStatusFilter? status,
        DateOnly? dueFrom,
        DateOnly? dueTo,
        long? categoryId,
        long? partnerId,
        string? sort,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service
            .ListAsync(new FinancialEntryListQuery(kind, search, status, dueFrom, dueTo, categoryId, partnerId, sort, page, pageSize), cancellationToken)
            .ConfigureAwait(false));

    private static async Task<Ok<FinancialSummaryDto>> SummaryAsync(
        FinancialEntryService service,
        FinancialEntryKind kind,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.SummaryAsync(kind, cancellationToken).ConfigureAwait(false));

    private static async Task<Ok<FinancialEntryDto>> GetEntryAsync(long id, FinancialEntryService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAsync(id, cancellationToken).ConfigureAwait(false));

    private static async Task<Ok<IReadOnlyList<FinancialEntryDto>>> ListInstallmentsAsync(long id, FinancialEntryService service, CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ListInstallmentsAsync(id, cancellationToken).ConfigureAwait(false));

    private static async Task<Created<IReadOnlyList<FinancialEntryDto>>> CreateEntryAsync(
        CreateFinancialEntryRequest request,
        FinancialEntryService service,
        CancellationToken cancellationToken)
    {
        var entries = await service.CreateAsync(request, cancellationToken).ConfigureAwait(false);
        return TypedResults.Created($"/api/v1/financial-entries/{entries[0].Id}", entries);
    }

    private static async Task<Ok<FinancialEntryDto>> UpdateEntryAsync(
        long id,
        UpdateFinancialEntryRequest request,
        FinancialEntryService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken).ConfigureAwait(false));

    private static async Task<Ok<FinancialEntryDto>> PayAsync(
        long id,
        PayFinancialEntryRequest request,
        FinancialEntryService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.PayAsync(id, request, cancellationToken).ConfigureAwait(false));

    private static async Task<Ok<FinancialEntryDto>> ReversePaymentAsync(
        long id,
        ReversePaymentRequest request,
        FinancialEntryService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.ReversePaymentAsync(id, request, cancellationToken).ConfigureAwait(false));

    private static async Task<Ok<FinancialEntryDto>> CancelAsync(
        long id,
        CancelFinancialEntryRequest request,
        FinancialEntryService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.CancelAsync(id, request, cancellationToken).ConfigureAwait(false));
}
