using System.Diagnostics;
using System.Globalization;

namespace Erp.Seed;

/// <summary>
/// Mede o p95 da listagem de produtos contra a API em execução, com cenários de busca, filtro,
/// ordenação e página profunda. Meta da Fase 2: p95 abaixo de 200 ms com o seed completo.
/// </summary>
internal sealed class ListingBenchmark(Uri baseUrl, int requestsPerScenario)
{
    private const double TargetP95Milliseconds = 200;

    private static readonly (string Name, string Query)[] Scenarios =
    [
        ("primeira página", "page=1&pageSize=20"),
        ("busca por nome", "search={name}&pageSize=20"),
        ("busca por SKU", "search={sku}&pageSize=20"),
        ("filtro categoria", "categoryId={category}&pageSize=20"),
        ("filtro inativos", "active=false&pageSize=20"),
        ("ordem por preço desc", "sort=-salePrice&pageSize=20"),
        ("página 100, 50 itens", "page=100&pageSize=50"),
        ("busca + categoria + ordem", "search={name}&categoryId={category}&sort=-salePrice&pageSize=20"),
    ];

    private static readonly string[] NamePrefixes = ["Ca", "Mo", "Fa", "Re", "Pe", "In", "Le", "Sa", "Gr", "Ba"];
    private static readonly string[] SkuPrefixes = ["ALI-00", "ELE-01", "INF-02", "MOV-0", "PET-00", "BEB-03"];

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient { BaseAddress = baseUrl, Timeout = TimeSpan.FromSeconds(30) };
        var random = new Random(42);
        var all = new List<double>();

        // Aquecimento: conexões, JIT e cache de planos.
        for (var i = 0; i < 20; i++)
        {
            await GetAsync(client, Build(Scenarios[i % Scenarios.Length].Query, random), cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"{"Cenário",-28} {"p50",8} {"p95",8} {"p99",8}");
        foreach (var (name, query) in Scenarios)
        {
            var timings = new List<double>(requestsPerScenario);
            for (var i = 0; i < requestsPerScenario; i++)
            {
                timings.Add(await GetAsync(client, Build(query, random), cancellationToken).ConfigureAwait(false));
            }

            all.AddRange(timings);
            Console.WriteLine($"{name,-28} {Percentile(timings, 50),6:F1}ms {Percentile(timings, 95),6:F1}ms {Percentile(timings, 99),6:F1}ms");
        }

        var p95 = Percentile(all, 95);
        var passed = p95 < TargetP95Milliseconds;
        Console.WriteLine($"{"TOTAL",-28} {Percentile(all, 50),6:F1}ms {p95,6:F1}ms {Percentile(all, 99),6:F1}ms");
        Console.WriteLine(passed
            ? $"OK: p95 {p95:F1} ms < {TargetP95Milliseconds} ms"
            : $"FALHOU: p95 {p95:F1} ms >= {TargetP95Milliseconds} ms");
        return passed ? 0 : 1;
    }

    private static string Build(string query, Random random) =>
        "/api/v1/products?" + query
            .Replace("{name}", NamePrefixes[random.Next(NamePrefixes.Length)], StringComparison.Ordinal)
            .Replace("{sku}", SkuPrefixes[random.Next(SkuPrefixes.Length)], StringComparison.Ordinal)
            .Replace("{category}", random.Next(1, 31).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

    private static async Task<double> GetAsync(HttpClient client, string url, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var response = await client.GetAsync(new Uri(url, UriKind.Relative), cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        _ = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    private static double Percentile(List<double> values, int percentile)
    {
        var sorted = values.Order().ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Clamp(index, 0, sorted.Count - 1)];
    }
}
