using Bogus;
using Erp.Domain.Partners;
using Erp.Infrastructure.Persistence;

namespace Erp.Seed;

/// <summary>
/// Clientes e fornecedores sintéticos. CPF e CNPJ saem de números aleatórios com os dígitos
/// verificadores calculados (nunca de pessoas ou empresas reais); parte dos CNPJ no formato
/// alfanumérico de 2026. Distribuição: 70% clientes, 20% fornecedores, 10% ambos.
/// </summary>
internal sealed class PartnerSeeder(Func<ErpDbContext> createContext, int count, int seed)
{
    private const int BatchSize = 5_000;
    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var faker = new Faker("pt_BR") { Random = new Randomizer(seed) };
        var documents = new HashSet<string>();

        for (var start = 0; start < count; start += BatchSize)
        {
            var db = createContext();
            await using (db.ConfigureAwait(false))
            {
                db.ChangeTracker.AutoDetectChangesEnabled = false;
                var batch = Math.Min(BatchSize, count - start);
                for (var i = 0; i < batch; i++)
                {
                    var type = faker.Random.WeightedRandom(
                        [PartnerType.Customer, PartnerType.Supplier, PartnerType.Both],
                        [0.7f, 0.2f, 0.1f]);
                    // Fornecedor costuma ser empresa; cliente, na maioria, pessoa física.
                    var company = type != PartnerType.Customer || faker.Random.Bool(0.25f);
                    string document;
                    do
                    {
                        document = company ? NewCnpj(faker.Random, alphanumeric: faker.Random.Bool(0.2f)) : NewCpf(faker.Random);
                    }
                    while (!documents.Add(document));

                    var name = company ? faker.Company.CompanyName() : faker.Name.FullName();
                    var partner = Partner.Create(new PartnerData(
                        type,
                        name,
                        document,
                        faker.Random.Bool(0.8f) ? faker.Internet.Email(name.Split(' ')[0]) : null,
                        faker.Random.Bool(0.7f) ? faker.Phone.PhoneNumber("(##) 9####-####") : null));
                    if (faker.Random.Bool(0.05f))
                    {
                        partner.Deactivate();
                    }

                    db.Partners.Add(partner);
                }

                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                Console.WriteLine($"  {start + batch:N0}/{count:N0} parceiros");
            }
        }
    }

    private static string NewCpf(Randomizer random)
    {
        var digits = string.Concat(Enumerable.Range(0, 9).Select(_ => (char)('0' + random.Number(9))));
        digits += TaxDocument.CpfCheckDigit(digits);
        return digits + TaxDocument.CpfCheckDigit(digits);
    }

    private static string NewCnpj(Randomizer random, bool alphanumeric)
    {
        var root = string.Concat(Enumerable.Range(0, 8).Select(_ => alphanumeric && random.Bool(0.5f)
            ? Letters[random.Number(Letters.Length - 1)]
            : (char)('0' + random.Number(9))));
        var body = root + "0001";
        body += TaxDocument.CnpjCheckDigit(body);
        return body + TaxDocument.CnpjCheckDigit(body);
    }
}
