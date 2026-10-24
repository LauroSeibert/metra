using Erp.Domain.Common;
using Erp.Domain.Products;

namespace Erp.UnitTests.Products;

public sealed class ProductTests
{
    private static ProductData Valid(
        string sku = "abc-001",
        string name = "Caneta azul",
        decimal costPrice = 1.50m,
        decimal salePrice = 3.90m,
        decimal minStock = 10m) =>
        new(sku, name, "  Descrição  ", 1, 2, costPrice, salePrice, minStock);

    [Fact]
    public void Create_NormalizesSkuAndTrimsText_AndStartsActive()
    {
        var product = Product.Create(Valid(sku: "  abc-001 ", name: "  Caneta azul "));

        Assert.Equal("ABC-001", product.Sku);
        Assert.Equal("Caneta azul", product.Name);
        Assert.Equal("Descrição", product.Description);
        Assert.True(product.Active);
    }

    [Fact]
    public void Create_BlankDescription_IsStoredAsNull()
    {
        var product = Product.Create(Valid() with { Description = "   " });

        Assert.Null(product.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankSku_IsPendingGeneration(string? sku)
    {
        var product = Product.Create(Valid() with { Sku = sku });

        Assert.True(product.HasPendingSku);
    }

    [Fact]
    public void Create_TypedSku_IsNotPending()
    {
        Assert.False(Product.Create(Valid()).HasPendingSku);
    }

    [Fact]
    public void AssignGeneratedSku_WithoutPendingSku_Throws()
    {
        var product = Product.Create(Valid());

        Assert.Throws<InvalidOperationException>(() => product.AssignGeneratedSku("Livros"));
    }

    [Theory]
    [InlineData("Livros", 33488, 1, "LIV-033488")]
    [InlineData("Eletrônicos", 7, 1, "ELE-000007")]
    [InlineData("Bebês", 1234567, 1, "BEB-1234567")]
    [InlineData("Pé de moleque", 1, 1, "PED-000001")]
    [InlineData("3M", 42, 1, "3M-000042")]
    [InlineData("--", 42, 1, "PRD-000042")]
    [InlineData(null, 42, 1, "PRD-000042")]
    [InlineData("Livros", 33488, 2, "LIV-033488-2")]
    public void GenerateSku_UsesCategoryPrefixAndId(string? category, long id, int attempt, string expected)
    {
        Assert.Equal(expected, Product.GenerateSku(category, id, attempt));
    }

    [Theory]
    [InlineData("-ABC")]
    [InlineData("AB C")]
    [InlineData("ABC%")]
    public void Create_InvalidSku_ReportsSkuField(string sku)
    {
        var error = Assert.Throws<DomainValidationException>(() => Product.Create(Valid(sku: sku)));

        Assert.Contains("sku", error.Errors.Keys);
    }

    [Fact]
    public void Create_SkuLongerThanLimit_ReportsSkuField()
    {
        var error = Assert.Throws<DomainValidationException>(() =>
            Product.Create(Valid(sku: new string('A', Product.SkuMaxLength + 1))));

        Assert.Contains("sku", error.Errors.Keys);
    }

    [Fact]
    public void Create_NegativePrices_ReportsBothFields()
    {
        var error = Assert.Throws<DomainValidationException>(() =>
            Product.Create(Valid(costPrice: -0.01m, salePrice: -1m)));

        Assert.Contains("costPrice", error.Errors.Keys);
        Assert.Contains("salePrice", error.Errors.Keys);
    }

    [Fact]
    public void Create_ZeroSalePrice_IsAllowed()
    {
        var product = Product.Create(Valid(salePrice: 0m));

        Assert.Equal(0m, product.SalePrice);
    }

    [Fact]
    public void Create_TooManyDecimals_ReportsField()
    {
        var error = Assert.Throws<DomainValidationException>(() =>
            Product.Create(Valid(salePrice: 1.005m, minStock: 1.0001m)));

        Assert.Contains("salePrice", error.Errors.Keys);
        Assert.Contains("minStock", error.Errors.Keys);
    }

    [Fact]
    public void Create_InvalidReferencesAndName_ReportsEveryField()
    {
        var error = Assert.Throws<DomainValidationException>(() =>
            Product.Create(new ProductData("A1", "", null, 0, -1, 0, 0, 0)));

        Assert.Equal(["categoryId", "name", "unitId"], error.Errors.Keys.Order());
    }

    [Fact]
    public void Create_WithoutCategoryAndUnit_IsAllowed()
    {
        var product = Product.Create(Valid() with { CategoryId = null, UnitId = null });

        Assert.Null(product.CategoryId);
        Assert.Null(product.UnitId);
    }

    [Fact]
    public void Update_InvalidData_KeepsPreviousValues()
    {
        var product = Product.Create(Valid());

        Assert.Throws<DomainValidationException>(() => product.Update(Valid(name: "")));

        Assert.Equal("Caneta azul", product.Name);
    }

    [Fact]
    public void DeactivateAndActivate_ToggleActive()
    {
        var product = Product.Create(Valid());

        product.Deactivate();
        Assert.False(product.Active);

        product.Activate();
        Assert.True(product.Active);
    }
}
