using System.Globalization;
using Erp.Seed;
using Microsoft.Extensions.Configuration;

// Uso:
//   dotnet run --project backend/src/Erp.Seed -- seed --profile small|full
//   dotnet run --project backend/src/Erp.Seed -- measure --url http://localhost:5080 --requests 300 --target products|stock|finance|all
// O measure entra na API com ERP_EMAIL e ERP_PASSWORD (variáveis de ambiente; o Admin do .env serve).
// Connection string: ConnectionStrings__Erp (variável de ambiente) ou user-secrets da API.
var command = args.Length > 0 ? args[0] : "seed";
var options = CommandLine.Parse(args.Skip(1));

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

switch (command)
{
    case "seed":
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets<SeedRunner>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("Erp");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            await Console.Error.WriteLineAsync("Defina ConnectionStrings__Erp ou o user-secret ConnectionStrings:Erp.").ConfigureAwait(false);
            return 1;
        }

        var profile = SeedProfile.From(options.GetValueOrDefault("profile", "small"));
        return await new SeedRunner(connectionString, profile).RunAsync(cancellation.Token).ConfigureAwait(false);

    case "measure":
        var url = new Uri(options.GetValueOrDefault("url", "http://localhost:5080"));
        var requests = int.Parse(options.GetValueOrDefault("requests", "300"), CultureInfo.InvariantCulture);
        var email = Environment.GetEnvironmentVariable("ERP_EMAIL");
        var password = Environment.GetEnvironmentVariable("ERP_PASSWORD");
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            await Console.Error.WriteLineAsync("Defina ERP_EMAIL e ERP_PASSWORD (um usuário da API) para medir.").ConfigureAwait(false);
            return 1;
        }

        var target = options.GetValueOrDefault("target", "all");
        return await new ListingBenchmark(url, requests, email, password, target).RunAsync(cancellation.Token).ConfigureAwait(false);

    default:
        await Console.Error.WriteLineAsync($"Comando desconhecido: {command}. Use seed ou measure.").ConfigureAwait(false);
        return 1;
}
