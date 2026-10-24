using Erp.Domain.Common;
using Erp.Domain.Finance;

namespace Erp.UnitTests.Finance;

public sealed class FinancialEntryTests
{
    private static readonly DateOnly Today = new(2026, 10, 24);
    private static readonly DateTime Now = new(2026, 10, 24, 15, 0, 0, DateTimeKind.Utc);

    private static FinancialEntryData Data(decimal amount = 100m, DateOnly? due = null) =>
        new(FinancialEntryKind.Payable, "  Aluguel  ", 1, null, amount, due ?? new DateOnly(2026, 1, 31));

    private static FinancialEntry Single(decimal amount = 100m) => FinancialEntry.CreateInstallments(Data(amount), 1)[0];

    [Fact]
    public void Installments_SplitTheTotal_WithTheRemainderInTheFirst_AndMonthlyDueDates()
    {
        var entries = FinancialEntry.CreateInstallments(Data(100m), 3);

        Assert.Equal([33.34m, 33.33m, 33.33m], entries.Select(e => e.Amount));
        Assert.Equal(100m, entries.Sum(e => e.Amount));
        // 31/01 + 1 mês = 28/02 (fim de mês), + 2 meses = 31/03.
        Assert.Equal([new DateOnly(2026, 1, 31), new DateOnly(2026, 2, 28), new DateOnly(2026, 3, 31)], entries.Select(e => e.DueDate));
        Assert.All(entries, e => Assert.Equal((3, "Aluguel", FinancialEntryStatus.Open), (e.InstallmentTotal, e.Description, e.Status)));
        Assert.Equal([1, 2, 3], entries.Select(e => e.InstallmentNo));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    public void Installments_OutOfRange_AreRejected(int installments)
    {
        var error = Assert.Throws<DomainValidationException>(() => FinancialEntry.CreateInstallments(Data(), installments));

        Assert.Contains("installments", error.Errors.Keys);
    }

    [Fact]
    public void Installments_SmallerThanACent_AreRejected()
    {
        var error = Assert.Throws<DomainValidationException>(() => FinancialEntry.CreateInstallments(Data(0.02m), 3));

        Assert.Contains("amount", error.Errors.Keys);
    }

    [Fact]
    public void Cycle_OpenPaidReversedOpen()
    {
        var entry = Single();

        entry.Pay(Today, 98.5m, Today);
        Assert.Equal((FinancialEntryStatus.Paid, Today, 98.5m), (entry.Status, entry.PaidAt, entry.PaidAmount));

        entry.ReversePayment("  cheque devolvido ", Now);
        Assert.Equal(FinancialEntryStatus.Open, entry.Status);
        Assert.Null(entry.PaidAt);
        Assert.Null(entry.PaidAmount);
        Assert.Equal(("cheque devolvido", Now), (entry.ReversalReason, entry.ReversedAt));
    }

    [Fact]
    public void PaidEntry_CannotBeEditedNorCancelled_UntilThePaymentIsReversed()
    {
        var entry = Single();
        entry.Pay(Today, 100m, Today);

        Assert.Throws<InvalidStateException>(() => entry.Update(Data(50m)));
        Assert.Throws<InvalidStateException>(() => entry.Cancel(Now));
        Assert.Throws<InvalidStateException>(() => entry.Pay(Today, 100m, Today));

        entry.ReversePayment(null, Now);
        entry.Cancel(Now);
        Assert.Equal(FinancialEntryStatus.Cancelled, entry.Status);
        Assert.Throws<InvalidStateException>(() => entry.ReversePayment(null, Now));
    }

    [Fact]
    public void Pay_RejectsFutureDateAndZeroAmount()
    {
        var entry = Single();

        var error = Assert.Throws<DomainValidationException>(() => entry.Pay(Today.AddDays(1), 0m, Today));

        Assert.Equal(["paidAmount", "paidAt"], error.Errors.Keys.Order());
    }

    [Fact]
    public void Overdue_IsAnOpenEntryWithAPastDueDate()
    {
        var entry = FinancialEntry.CreateInstallments(Data(due: Today.AddDays(-1)), 1)[0];

        Assert.True(entry.IsOverdue(Today));
        Assert.False(entry.IsOverdue(Today.AddDays(-1)));
        entry.Pay(Today, 100m, Today);
        Assert.False(entry.IsOverdue(Today));
    }

    [Fact]
    public void Update_CannotChangeTheKind()
    {
        var entry = Single();

        var error = Assert.Throws<DomainValidationException>(() =>
            entry.Update(Data() with { Kind = FinancialEntryKind.Receivable }));

        Assert.Contains("kind", error.Errors.Keys);
    }

    [Fact]
    public void Category_FitsOnlyItsKindOfEntry()
    {
        var revenue = FinancialCategory.Create("Vendas", FinancialCategoryKind.Revenue);

        Assert.True(revenue.Fits(FinancialEntryKind.Receivable));
        Assert.False(revenue.Fits(FinancialEntryKind.Payable));
    }
}
