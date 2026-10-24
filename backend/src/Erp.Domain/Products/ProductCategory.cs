using Erp.Domain.Common;

namespace Erp.Domain.Products;

/// <summary>Categoria de produto. Nome único; não é excluída enquanto houver produto nela.</summary>
public sealed class ProductCategory : Entity
{
    public const int NameMaxLength = 100;

    private ProductCategory()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public static ProductCategory Create(string name)
    {
        var category = new ProductCategory();
        category.Rename(name);
        return category;
    }

    public void Rename(string name)
    {
        new ValidationErrors().Required("name", name, NameMaxLength).ThrowIfAny();
        Name = name.Trim();
    }
}
