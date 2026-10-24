using Erp.Domain.Common;

namespace Erp.Domain.Products;

/// <summary>Unidade de medida (UN, KG, CX...). Código único; não é excluída enquanto houver produto nela.</summary>
public sealed class Unit : Entity
{
    public const int CodeMaxLength = 10;
    public const int NameMaxLength = 60;

    private Unit()
    {
    }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public static Unit Create(string code, string name)
    {
        var unit = new Unit();
        unit.Update(code, name);
        return unit;
    }

    public static string NormalizeCode(string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return code.Trim().ToUpperInvariant();
    }

    public void Update(string code, string name)
    {
        var normalized = code is null ? null : NormalizeCode(code);
        new ValidationErrors()
            .Required("code", normalized, CodeMaxLength)
            .Required("name", name, NameMaxLength)
            .ThrowIfAny();

        Code = normalized!;
        Name = name.Trim();
    }
}
