using System.Diagnostics;
using Bogus;
using Erp.Domain.Products;
using Erp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Erp.Seed;

/// <summary>
/// Gera dados 100% sintéticos e reproduzíveis (semente fixa por módulo). Cada módulo só é gerado se a
/// tabela dele estiver vazia: rodar de novo completa o que falta sem misturar seed com dados digitados.
/// </summary>
internal sealed class SeedRunner(string connectionString, SeedProfile profile)
{
    private const int RandomSeed = 20261023;
    private const int BatchSize = 5_000;

    private static readonly (string Code, string Name)[] UnitData =
    [
        ("UN", "Unidade"), ("KG", "Quilograma"), ("G", "Grama"), ("L", "Litro"), ("ML", "Mililitro"),
        ("M", "Metro"), ("CX", "Caixa"), ("PCT", "Pacote"), ("PAR", "Par"), ("DZ", "Dúzia"),
    ];

    private static readonly (string Prefix, string Name)[] CategoryData =
    [
        ("ALI", "Alimentos"), ("BEB", "Bebidas"), ("LIM", "Limpeza"), ("HIG", "Higiene pessoal"),
        ("PAP", "Papelaria"), ("INF", "Informática"), ("ELE", "Eletrônicos"), ("ELD", "Eletrodomésticos"),
        ("MOV", "Móveis"), ("DEC", "Decoração"), ("CAM", "Cama, mesa e banho"), ("UTI", "Utilidades domésticas"),
        ("FER", "Ferramentas"), ("CON", "Material de construção"), ("ILU", "Iluminação"), ("JAR", "Jardinagem"),
        ("AUT", "Automotivo"), ("ESP", "Esporte e lazer"), ("BRI", "Brinquedos"), ("BEB2", "Bebês"),
        ("PET", "Pet shop"), ("MOD", "Moda"), ("CAL", "Calçados"), ("ACE", "Acessórios"),
        ("BEL", "Beleza"), ("SAU", "Saúde"), ("LIV", "Livros"), ("MUS", "Instrumentos musicais"),
        ("ESC", "Escritório"), ("EMB", "Embalagens"),
    ];

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        Randomizer.Seed = new Random(RandomSeed);

        bool hasProducts;
        bool hasPartners;
        await using (var db = CreateContext())
        {
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            hasProducts = await db.Products.AnyAsync(cancellationToken).ConfigureAwait(false);
            hasPartners = await db.Partners.AnyAsync(cancellationToken).ConfigureAwait(false);
        }

        if (hasProducts)
        {
            Console.WriteLine("Produtos: a base já tem, pulando.");
        }
        else
        {
            var (categories, units) = await SeedCatalogAsync(cancellationToken).ConfigureAwait(false);
            await SeedProductsAsync(categories, units, cancellationToken).ConfigureAwait(false);
        }

        if (hasPartners)
        {
            Console.WriteLine("Parceiros: a base já tem, pulando.");
        }
        else
        {
            await new PartnerSeeder(CreateContext, profile.Partners, RandomSeed + 1).RunAsync(cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"Seed '{profile.Name}' concluído em {stopwatch.Elapsed:mm\\:ss}.");
        return 0;
    }

    private async Task<(List<(long Id, string Prefix)> Categories, List<long> Units)> SeedCatalogAsync(
        CancellationToken cancellationToken)
    {
        await using var db = CreateContext();

        var categories = CategoryData.Select(c => (Entity: ProductCategory.Create(c.Name), c.Prefix)).ToList();
        var units = UnitData.Select(u => Unit.Create(u.Code, u.Name)).ToList();
        db.ProductCategories.AddRange(categories.Select(c => c.Entity));
        db.Units.AddRange(units);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return (categories.Select(c => (c.Entity.Id, c.Prefix)).ToList(), units.Select(u => u.Id).ToList());
    }

    private async Task SeedProductsAsync(
        List<(long Id, string Prefix)> categories,
        List<long> units,
        CancellationToken cancellationToken)
    {
        var faker = new Faker("pt_BR");
        for (var start = 0; start < profile.Products; start += BatchSize)
        {
            await using var db = CreateContext();
            db.ChangeTracker.AutoDetectChangesEnabled = false;

            var count = Math.Min(BatchSize, profile.Products - start);
            for (var i = 0; i < count; i++)
            {
                var number = start + i + 1;
                var category = faker.PickRandom(categories);
                var cost = Math.Round(faker.Random.Decimal(1m, 2_000m), 2);
                var product = Product.Create(new ProductData(
                    Sku: $"{category.Prefix}-{number:D6}",
                    Name: $"{faker.Commerce.ProductName()} {faker.Commerce.ProductMaterial()}",
                    Description: faker.Random.Bool(0.7f) ? faker.Commerce.ProductDescription() : null,
                    CategoryId: category.Id,
                    UnitId: faker.PickRandom(units),
                    CostPrice: cost,
                    SalePrice: Math.Round(cost * faker.Random.Decimal(1.2m, 2.0m), 2),
                    MinStock: faker.Random.Int(0, 50)));
                if (faker.Random.Bool(0.08f))
                {
                    product.Deactivate();
                }

                db.Products.Add(product);
            }

            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"  {start + count:N0}/{profile.Products:N0} produtos");
        }
    }

    private ErpDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ErpDbContext>().UseMySQL(connectionString).Options, TimeProvider.System);
}
