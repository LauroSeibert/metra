using Erp.Domain.Common;
using Erp.Domain.Partners;

namespace Erp.UnitTests.Partners;

public sealed class TaxDocumentTests
{
    // Números públicos de exemplo, com dígitos válidos (não pertencem a pessoas nem empresas reais).
    [Theory]
    [InlineData("52998224725")]
    [InlineData("11144477735")]
    public void ValidCpf_IsAccepted(string cpf)
    {
        Assert.True(TaxDocument.IsValid(cpf));
        Assert.Equal(TaxDocumentKind.Cpf, TaxDocument.KindOf(cpf));
    }

    [Theory]
    [InlineData("52998224724")]
    [InlineData("11111111111")]
    [InlineData("5299822472A")]
    public void InvalidCpf_IsRejected(string cpf)
    {
        Assert.False(TaxDocument.IsValid(cpf));
    }

    [Theory]
    [InlineData("11222333000181")]
    [InlineData("12ABC34501DE35")] // exemplo oficial do CNPJ alfanumérico (Receita Federal)
    public void ValidCnpj_NumericAndAlphanumeric_IsAccepted(string cnpj)
    {
        Assert.True(TaxDocument.IsValid(cnpj));
        Assert.Equal(TaxDocumentKind.Cnpj, TaxDocument.KindOf(cnpj));
    }

    [Theory]
    [InlineData("11222333000182")]
    [InlineData("00000000000000")]
    [InlineData("12ABC34501DE3A")]
    [InlineData("12ABC34501DE36")]
    public void InvalidCnpj_IsRejected(string cnpj)
    {
        Assert.False(TaxDocument.IsValid(cnpj));
    }

    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData(" 12.abc.345/01de-35 ", "12ABC34501DE35")]
    public void Normalize_StripsPunctuation_AndUppercases(string input, string expected)
    {
        Assert.Equal(expected, TaxDocument.Normalize(input));
    }

    [Theory]
    [InlineData("52998224725", "529.982.247-25")]
    [InlineData("12ABC34501DE35", "12.ABC.345/01DE-35")]
    public void Format_UsesTheOfficialMask(string normalized, string expected)
    {
        Assert.Equal(expected, TaxDocument.Format(normalized));
    }
}

public sealed class PartnerTests
{
    private static PartnerData Valid(string? document = "529.982.247-25", string? email = null) =>
        new(PartnerType.Customer, "  Maria Silva  ", document, email, " (11) 4000-0000 ");

    [Fact]
    public void Create_NormalizesFields_AndStartsActive()
    {
        var partner = Partner.Create(Valid(email: " Maria@Exemplo.COM "));

        Assert.Equal("Maria Silva", partner.Name);
        Assert.Equal("52998224725", partner.Document);
        Assert.Equal(TaxDocumentKind.Cpf, partner.DocumentKind);
        Assert.Equal("maria@exemplo.com", partner.Email);
        Assert.Equal("(11) 4000-0000", partner.Phone);
        Assert.True(partner.Active);
        Assert.True(partner.IsCustomer);
        Assert.False(partner.IsSupplier);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("529.982.247-24")]
    public void Create_MissingOrInvalidDocument_ReportsDocument(string? document)
    {
        var error = Assert.Throws<DomainValidationException>(() => Partner.Create(Valid(document)));

        Assert.Contains("document", error.Errors.Keys);
    }

    [Fact]
    public void Create_InvalidEmailAndType_ReportsBoth()
    {
        var error = Assert.Throws<DomainValidationException>(() =>
            Partner.Create(Valid(email: "sem-arroba") with { Type = (PartnerType)9 }));

        Assert.Equal(["email", "type"], error.Errors.Keys.Order());
    }

    [Fact]
    public void Both_IsCustomerAndSupplier()
    {
        var partner = Partner.Create(Valid() with { Type = PartnerType.Both });

        Assert.True(partner.IsCustomer);
        Assert.True(partner.IsSupplier);
    }
}
