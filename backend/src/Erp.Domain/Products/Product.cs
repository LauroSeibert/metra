using System.Globalization;
using System.Text.RegularExpressions;
using Erp.Domain.Common;

namespace Erp.Domain.Products;

/// <summary>
/// Produto vendável. Regras: SKU único (garantido pelo banco e pelo caso de uso) e opcional na
/// entrada: em branco, é gerado a partir da categoria e do id; categoria e unidade opcionais;
/// preços não negativos e nunca excluído: produto fora de uso é inativado.
/// </summary>
public sealed partial class Product : Entity
{
    public const int SkuMaxLength = 40;
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 1000;

    /// <summary>Prefixo do SKU gerado sem categoria ou quando o nome dela não tem letras nem números.</summary>
    public const string FallbackSkuPrefix = "PRD";

    /// <summary>Marca o SKU provisório, que nunca passa na validação e por isso não colide com SKU digitado.</summary>
    private const char PendingSkuMarker = '~';

    private Product()
    {
    }

    public string Sku { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public long? CategoryId { get; private set; }

    public ProductCategory? Category { get; private set; }

    public long? UnitId { get; private set; }

    public Unit? Unit { get; private set; }

    public decimal CostPrice { get; private set; }

    public decimal SalePrice { get; private set; }

    public decimal MinStock { get; private set; }

    public bool Active { get; private set; }

    /// <summary>SKU veio em branco e ainda espera <see cref="AssignGeneratedSku"/>.</summary>
    public bool HasPendingSku => Sku.Length > 0 && Sku[0] == PendingSkuMarker;

    public static Product Create(ProductData data)
    {
        var product = new Product { Active = true };
        product.Apply(data);
        return product;
    }

    public void Update(ProductData data) => Apply(data);

    public void Activate() => Active = true;

    public void Deactivate() => Active = false;

    /// <summary>SKU é comparado sem espaços nas pontas e em maiúsculas.</summary>
    public static string NormalizeSku(string sku)
    {
        ArgumentNullException.ThrowIfNull(sku);
        return sku.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// SKU gerado: três primeiras letras ou números do nome da categoria, sem acento, e o id com
    /// ao menos seis dígitos ("Livros", 33488 → LIV-033488). <paramref name="attempt"/> acima de 1
    /// acrescenta um sufixo, para o raro caso de alguém já ter digitado o mesmo código.
    /// </summary>
    public static string GenerateSku(string? categoryName, long id, int attempt = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(id);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(attempt);

        var letters = (categoryName ?? string.Empty)
            .Select(FoldAccent)
            .Where(char.IsAsciiLetterOrDigit)
            .Take(3)
            .ToArray();
        var prefix = letters.Length == 0 ? FallbackSkuPrefix : new string(letters).ToUpperInvariant();
        var sku = $"{prefix}-{id.ToString("D6", CultureInfo.InvariantCulture)}";
        return attempt == 1 ? sku : $"{sku}-{attempt.ToString(CultureInfo.InvariantCulture)}";
    }

    /// <summary>
    /// Tira o acento das letras do português. Tabela explícita porque o backend roda com
    /// InvariantGlobalization (Directory.Build.props), em que string.Normalize não decompõe acentos.
    /// </summary>
    private static char FoldAccent(char c)
    {
        var index = AccentedLetters.IndexOf(c, StringComparison.Ordinal);
        return index < 0 ? c : PlainLetters[index];
    }

    private const string AccentedLetters = "áàâãäéèêëíìîïóòôõöúùûüçñÁÀÂÃÄÉÈÊËÍÌÎÏÓÒÔÕÖÚÙÛÜÇÑ";
    private const string PlainLetters = "aaaaaeeeeiiiiooooouuuucnAAAAAEEEEIIIIOOOOOUUUUCN";

    /// <summary>Troca o SKU provisório pelo gerado; exige o produto já gravado (id conhecido).</summary>
    public void AssignGeneratedSku(string? categoryName, int attempt = 1)
    {
        if (!HasPendingSku)
        {
            throw new InvalidOperationException("O produto não está aguardando SKU gerado.");
        }

        Sku = GenerateSku(categoryName, Id, attempt);
    }

    private void Apply(ProductData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var sku = string.IsNullOrWhiteSpace(data.Sku) ? null : NormalizeSku(data.Sku);
        var errors = new ValidationErrors()
            .MaxLength("sku", sku, SkuMaxLength)
            .Required("name", data.Name, NameMaxLength)
            .MaxLength("description", data.Description, DescriptionMaxLength)
            .OptionalPositiveId("categoryId", data.CategoryId)
            .OptionalPositiveId("unitId", data.UnitId)
            .NonNegativeDecimal("costPrice", data.CostPrice, DecimalLimits.MoneyScale, DecimalLimits.MoneyMax)
            .NonNegativeDecimal("salePrice", data.SalePrice, DecimalLimits.MoneyScale, DecimalLimits.MoneyMax)
            .NonNegativeDecimal("minStock", data.MinStock, DecimalLimits.QuantityScale, DecimalLimits.QuantityMax);

        if (!string.IsNullOrEmpty(sku) && sku.Length <= SkuMaxLength && !SkuPattern().IsMatch(sku))
        {
            errors.Add("sku", "Use letras, números, ponto, hífen ou sublinhado, começando por letra ou número.");
        }

        errors.ThrowIfAny();

        // Guid evita colisão no índice único entre inserções simultâneas antes de o SKU ser gerado.
        Sku = sku ?? $"{PendingSkuMarker}{Guid.NewGuid():N}";
        Name = data.Name.Trim();
        Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim();
        CategoryId = data.CategoryId;
        UnitId = data.UnitId;
        CostPrice = data.CostPrice;
        SalePrice = data.SalePrice;
        MinStock = data.MinStock;
    }

    [GeneratedRegex("^[A-Z0-9][A-Z0-9._-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex SkuPattern();
}

/// <summary>Dados editáveis de um produto. <see cref="Sku"/> em branco pede SKU gerado.</summary>
public sealed record ProductData(
    string? Sku,
    string Name,
    string? Description,
    long? CategoryId,
    long? UnitId,
    decimal CostPrice,
    decimal SalePrice,
    decimal MinStock);
