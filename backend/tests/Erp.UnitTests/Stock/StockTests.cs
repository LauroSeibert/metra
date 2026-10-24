using Erp.Domain.Common;
using Erp.Domain.Stock;

namespace Erp.UnitTests.Stock;

public sealed class StockMovementTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Entry_IsPositive_AndExitIsStoredNegative()
    {
        var entry = StockMovement.Entry(1, 10m, 2.5m, Now, 7, "  compra  ");
        var exit = StockMovement.Exit(1, 4m, null, Now, 7, null);

        Assert.Equal((StockMovementType.Entry, 10m, "compra"), (entry.Type, entry.Quantity, entry.Note));
        Assert.Equal((StockMovementType.Exit, -4m), (exit.Type, exit.Quantity));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1.0001)]
    public void EntryAndExit_RequirePositiveQuantityWithThreeDecimals(double quantity)
    {
        Assert.Throws<DomainValidationException>(() => StockMovement.Entry(1, (decimal)quantity, null, Now, 7, null));
        Assert.Throws<DomainValidationException>(() => StockMovement.Exit(1, (decimal)quantity, null, Now, 7, null));
    }

    [Fact]
    public void Adjustment_RecordsTheDifferenceToTheCount()
    {
        var down = StockMovement.Adjustment(1, 8m, 10m, Now, 7, "inventário");
        var up = StockMovement.Adjustment(1, 12.5m, 10m, Now, 7, "inventário");

        Assert.Equal(-2m, down.Quantity);
        Assert.Equal(2.5m, up.Quantity);
    }

    [Fact]
    public void Adjustment_WithoutDifferenceOrReason_IsRejected()
    {
        var same = Assert.Throws<DomainValidationException>(() => StockMovement.Adjustment(1, 10m, 10m, Now, 7, "x"));
        var noReason = Assert.Throws<DomainValidationException>(() => StockMovement.Adjustment(1, 5m, 10m, Now, 7, " "));

        Assert.Contains("quantity", same.Errors.Keys);
        Assert.Contains("note", noReason.Errors.Keys);
    }

    [Fact]
    public void Reverse_UsesTheOppositeQuantity_AndCannotReverseAReversal()
    {
        var exit = StockMovement.Exit(1, 4m, 3m, Now, 7, null);

        var reversal = StockMovement.Reverse(exit, Now, 8, "lançado errado");

        Assert.Equal((StockMovementType.Reversal, 4m, 3m), (reversal.Type, reversal.Quantity, reversal.UnitCost));
        Assert.Equal(StockMovement.MovementReference, reversal.ReferenceType);
        Assert.Throws<DomainValidationException>(() => StockMovement.Reverse(reversal, Now, 8, null));
    }
}

public sealed class StockBalanceTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Apply_AddsTheSignedQuantity()
    {
        var balance = StockBalance.Empty(1);

        balance.Apply(StockMovement.Entry(1, 10m, null, Now, 7, null), allowNegative: false);
        balance.Apply(StockMovement.Exit(1, 3.5m, null, Now, 7, null), allowNegative: false);

        Assert.Equal(6.5m, balance.Quantity);
    }

    [Fact]
    public void Apply_RejectsNegativeBalance_UnlessAllowed()
    {
        var balance = StockBalance.Empty(1);
        var exit = StockMovement.Exit(1, 2m, null, Now, 7, null);

        var error = Assert.Throws<DomainValidationException>(() => balance.Apply(exit, allowNegative: false));
        Assert.Contains("quantity", error.Errors.Keys);
        Assert.Equal(0m, balance.Quantity);

        balance.Apply(exit, allowNegative: true);
        Assert.Equal(-2m, balance.Quantity);
    }

    [Fact]
    public void Apply_RejectsAMovementOfAnotherProduct()
    {
        var balance = StockBalance.Empty(1);

        Assert.Throws<InvalidOperationException>(() =>
            balance.Apply(StockMovement.Entry(2, 1m, null, Now, 7, null), allowNegative: false));
    }
}
