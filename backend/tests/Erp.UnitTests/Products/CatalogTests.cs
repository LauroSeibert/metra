using Erp.Domain.Common;
using Erp.Domain.Products;

namespace Erp.UnitTests.Products;

public sealed class CatalogTests
{
    [Fact]
    public void Unit_Create_NormalizesCode()
    {
        var unit = Unit.Create(" kg ", " Quilograma ");

        Assert.Equal("KG", unit.Code);
        Assert.Equal("Quilograma", unit.Name);
    }

    [Fact]
    public void Unit_CodeTooLong_ReportsCode()
    {
        var error = Assert.Throws<DomainValidationException>(() => Unit.Create("ABCDEFGHIJK", "Nome"));

        Assert.Contains("code", error.Errors.Keys);
    }

    [Fact]
    public void Category_BlankName_ReportsName()
    {
        var error = Assert.Throws<DomainValidationException>(() => ProductCategory.Create("  "));

        Assert.Contains("name", error.Errors.Keys);
    }

    [Fact]
    public void Category_Rename_TrimsName()
    {
        var category = ProductCategory.Create("Bebidas");

        category.Rename("  Bebidas geladas ");

        Assert.Equal("Bebidas geladas", category.Name);
    }
}
