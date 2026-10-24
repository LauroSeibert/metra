using Erp.Domain.Common;
using Erp.Domain.Sales;

namespace Erp.UnitTests.Sales;

public sealed class SaleTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 15, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = new(2026, 10, 24);

    private static SaleData Data(PaymentMethod method = PaymentMethod.CreditCard, int installments = 3, decimal discount = 0m) =>
        new(7, method, installments, discount, Today, null);

    private static SaleItemData[] Items() =>
    [
        new(1, 2m, 50m, 5m),   // 100 − 5 = 95
        new(2, 1.5m, 10m, 0m), // 15
    ];

    [Fact]
    public void Draft_ComputesItemTotalsSubtotalAndTotal()
    {
        var sale = Sale.CreateDraft(1, Data(discount: 5m), Items(), maxDiscountPercent: 10m);

        Assert.Equal([95m, 15m], sale.Items.Select(i => i.Total));
        Assert.Equal((115m, 110m, 105m), (sale.Gross, sale.Subtotal, sale.Total));
        Assert.Equal(SaleStatus.Draft, sale.Status);
    }

    [Fact]
    public void Discount_AboveTheConfiguredMaximum_IsRejected()
    {
        // 5 nos itens + 8 na venda = 13 de 115 (11,3%) > 10%.
        var error = Assert.Throws<DomainValidationException>(() => Sale.CreateDraft(1, Data(discount: 8m), Items(), 10m));

        Assert.Contains("discount", error.Errors.Keys);
        Assert.NotNull(Sale.CreateDraft(1, Data(discount: 8m), Items(), 12m));
    }

    [Fact]
    public void Confirm_ChecksTheDiscountAgainst_TheCurrentMaximum()
    {
        var sale = Sale.CreateDraft(1, Data(discount: 8m), Items(), 12m);

        Assert.Throws<DomainValidationException>(() => sale.Confirm(Now, 10m));
        sale.Confirm(Now, 12m);
        Assert.Equal((SaleStatus.Confirmed, Now), (sale.Status, sale.ConfirmedAt));
    }

    [Theory]
    [InlineData(PaymentMethod.Cash, 2)]
    [InlineData(PaymentMethod.Pix, 3)]
    [InlineData(PaymentMethod.CreditCard, 13)]
    [InlineData(PaymentMethod.BankSlip, 0)]
    public void Installments_MustFitThePaymentMethod(PaymentMethod method, int installments)
    {
        var error = Assert.Throws<DomainValidationException>(() => Sale.CreateDraft(1, Data(method, installments), Items(), 10m));

        Assert.Contains("installments", error.Errors.Keys);
    }

    [Fact]
    public void Items_AreRequired_AndValidated()
    {
        var empty = Assert.Throws<DomainValidationException>(() => Sale.CreateDraft(1, Data(), [], 10m));
        var bad = Assert.Throws<DomainValidationException>(() =>
            Sale.CreateDraft(1, Data(), [new SaleItemData(1, 0m, 10m, 0m), new SaleItemData(2, 1m, 10m, 11m)], 10m));

        Assert.Contains("items", empty.Errors.Keys);
        Assert.Equal(2, bad.Errors["items"].Length);
    }

    [Fact]
    public void OnlyTheDraft_IsEditedOrConfirmed_AndCancelledIsFinal()
    {
        var sale = Sale.CreateDraft(1, Data(), Items(), 10m);
        sale.Confirm(Now, 10m);

        Assert.Throws<InvalidStateException>(() => sale.UpdateDraft(Data(), Items(), 10m));
        Assert.Throws<InvalidStateException>(() => sale.Confirm(Now, 10m));

        sale.Cancel(Now, "  desistência ");
        Assert.Equal((SaleStatus.Cancelled, "desistência"), (sale.Status, sale.CancellationReason));
        Assert.Throws<InvalidStateException>(() => sale.Cancel(Now, null));
    }

    [Fact]
    public void ImmediateMethods_AreCashPixAndDebit()
    {
        Assert.True(Sale.IsImmediate(PaymentMethod.Pix));
        Assert.False(Sale.IsImmediate(PaymentMethod.BankSlip));
    }

    [Fact]
    public void Sequence_GivesConsecutiveNumbers()
    {
        var sequence = DocumentSequence.Create(DocumentSequence.Sale);

        Assert.Equal([1L, 2L, 3L], [sequence.Next(), sequence.Next(), sequence.Next()]);
    }
}
