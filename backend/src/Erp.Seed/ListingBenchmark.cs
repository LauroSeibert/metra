using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Erp.Seed;

/// <summary>
/// Mede o p95 das listagens contra a API em execução, com cenários de busca, filtro, ordenação e página
/// profunda. Alvos: `products` (Fase 2) e `stock` (Fase 4, histórico com 1 milhão de movimentações por
/// chave). Meta: p95 abaixo de 200 ms com o seed completo.
/// </summary>
internal sealed class ListingBenchmark(Uri baseUrl, int requestsPerScenario, string email, string password, string target = "all")
{
    private const double TargetP95Milliseconds = 200;

    private static readonly (string Target, string Name, string Url)[] AllScenarios =
    [
        ("products", "primeira página", "/api/v1/products?page=1&pageSize=20"),
        ("products", "busca por nome", "/api/v1/products?search={name}&pageSize=20"),
        ("products", "busca por SKU", "/api/v1/products?search={sku}&pageSize=20"),
        ("products", "filtro categoria", "/api/v1/products?categoryId={category}&pageSize=20"),
        ("products", "filtro inativos", "/api/v1/products?active=false&pageSize=20"),
        ("products", "ordem por preço desc", "/api/v1/products?sort=-salePrice&pageSize=20"),
        ("products", "página 100, 50 itens", "/api/v1/products?page=100&pageSize=50"),
        ("products", "busca + categoria + ordem", "/api/v1/products?search={name}&categoryId={category}&sort=-salePrice&pageSize=20"),
        ("products", "estoque baixo", "/api/v1/products?lowStock=true&pageSize=20"),
        ("stock", "histórico geral", "/api/v1/stock-movements?pageSize=50"),
        ("stock", "histórico de um produto", "/api/v1/stock-movements?productId={product}&pageSize=50"),
        ("stock", "histórico até uma data", "/api/v1/stock-movements?to={date}&pageSize=50"),
        ("stock", "produto + tipo saída", "/api/v1/stock-movements?productId={product}&type=Exit&pageSize=50"),
        ("stock", "saldo, primeira página", "/api/v1/stock-balances?pageSize=20"),
        ("stock", "saldo abaixo do mínimo", "/api/v1/stock-balances?lowStock=true&pageSize=20"),
        ("stock", "saldo por categoria", "/api/v1/stock-balances?categoryId={category}&sort=quantity&pageSize=20"),
    ];

    private (string Target, string Name, string Url)[] Scenarios =>
        [.. AllScenarios.Where(s => target == "all" || s.Target == target)];

    private static readonly string[] NamePrefixes = ["Ca", "Mo", "Fa", "Re", "Pe", "In", "Le", "Sa", "Gr", "Ba"];
    private static readonly string[] SkuPrefixes = ["ALI-00", "ELE-01", "INF-02", "MOV-0", "PET-00", "BEB-03"];

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient { BaseAddress = baseUrl, Timeout = TimeSpan.FromSeconds(30) };
        // A API exige sessão desde a Fase 3: entra uma vez e usa o token de acesso (15 min bastam).
        using (var login = await client.PostAsJsonAsync(new Uri("/api/v1/auth/login", UriKind.Relative), new { email, password }, cancellationToken).ConfigureAwait(false))
        {
            login.EnsureSuccessStatusCode();
            using var body = await JsonDocument.ParseAsync(await login.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false), cancellationToken: cancellationToken).ConfigureAwait(false);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.RootElement.GetProperty("accessToken").GetString());
        }

        var random = new Random(42);
        var all = new List<double>();

        // Aquecimento: conexões, JIT e cache de planos.
        for (var i = 0; i < 20; i++)
        {
            await GetAsync(client, Build(Scenarios[i % Scenarios.Length].Url, random), cancellationToken).ConfigureAwait(false);
        }

        Console.WriteLine($"{"Cenário",-28} {"p50",8} {"p95",8} {"p99",8}");
        foreach (var (_, name, url) in Scenarios)
        {
            var timings = new List<double>(requestsPerScenario);
            for (var i = 0; i < requestsPerScenario; i++)
            {
                timings.Add(await GetAsync(client, Build(url, random), cancellationToken).ConfigureAwait(false));
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

    private static string Build(string url, Random random) =>
        url
            .Replace("{name}", NamePrefixes[random.Next(NamePrefixes.Length)], StringComparison.Ordinal)
            .Replace("{sku}", SkuPrefixes[random.Next(SkuPrefixes.Length)], StringComparison.Ordinal)
            .Replace("{category}", random.Next(1, 31).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{product}", random.Next(1, 100_001).ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{date}", DateTime.UtcNow.AddDays(-random.Next(1, 720)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), StringComparison.Ordinal);

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
