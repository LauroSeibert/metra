using Erp.Application.Common;
using Erp.Domain.Common;
using Erp.Infrastructure.Persistence;

namespace Erp.UnitTests.Common;

public sealed class QueryInputTests
{
    private static readonly string[] Fields = ["name", "salePrice"];

    [Theory]
    [InlineData(null, null)]
    [InlineData("   ", null)]
    [InlineData("cam", "cam%")]
    [InlineData(" 100% ", "100!%%")]
    [InlineData("a_b", "a!_b%")]
    [InlineData("oi!", "oi!!%")]
    [InlineData("c:\\x", "c:\\x%")]
    public void SearchPattern_Prefix_EscapesWildcards(string? term, string? expected)
    {
        Assert.Equal(expected, SearchPattern.Prefix(term));
    }

    [Theory]
    [InlineData(null, "name", false)]
    [InlineData("salePrice", "salePrice", false)]
    [InlineData("-SALEPRICE", "salePrice", true)]
    public void SortRequest_Parse_AcceptsAllowedFields(string? sort, string field, bool descending)
    {
        var parsed = SortRequest.Parse(sort, "name", Fields);

        Assert.Equal(field, parsed.Field);
        Assert.Equal(descending, parsed.Descending);
    }

    [Fact]
    public void SortRequest_Parse_RejectsUnknownField()
    {
        var error = Assert.Throws<DomainValidationException>(() => SortRequest.Parse("cost_price; drop", "name", Fields));

        Assert.Contains("sort", error.Errors.Keys);
    }

    [Theory]
    [InlineData(0, 20, "page")]
    [InlineData(1, 0, "pageSize")]
    [InlineData(1, 101, "pageSize")]
    public void PageRequest_OutOfRange_ReportsField(int page, int pageSize, string field)
    {
        var error = Assert.Throws<DomainValidationException>(() => PageRequest.From(page, pageSize));

        Assert.Contains(field, error.Errors.Keys);
    }

    [Fact]
    public void PageRequest_Defaults_AndSkip()
    {
        Assert.Equal(new PageRequest(1, PageRequest.DefaultPageSize), PageRequest.From(null, null));
        Assert.Equal(200, PageRequest.From(3, 100).Skip);
    }

    [Theory]
    [InlineData("Product", "product")]
    [InlineData("ProductCategory", "product_category")]
    [InlineData("CategoryId", "category_id")]
    [InlineData("CreatedAt", "created_at")]
    [InlineData("SKUCode", "sku_code")]
    public void SnakeCase_ConvertsPascalCase(string name, string expected)
    {
        Assert.Equal(expected, SnakeCaseNaming.ToSnakeCase(name));
    }
}
