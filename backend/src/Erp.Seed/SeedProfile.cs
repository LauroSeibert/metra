namespace Erp.Seed;

/// <summary>Volumes do seed. <c>full</c> é o volume da demonstração de performance (docs/plan.md).</summary>
internal sealed record SeedProfile(string Name, int Products, int Partners, int StockMovements, int FinancialEntries, int Sales)
{
    public static SeedProfile From(string name) => name switch
    {
        "small" => new SeedProfile(name, 1_000, 500, 10_000, 2_000, 2_000),
        "full" => new SeedProfile(name, 100_000, 20_000, 1_000_000, 250_000, 10_000),
        _ => throw new ArgumentException($"Perfil desconhecido: {name}. Use small ou full.", nameof(name)),
    };
}
