namespace Erp.Seed;

internal static class CommandLine
{
    /// <summary>Lê pares <c>--chave valor</c>.</summary>
    public static Dictionary<string, string> Parse(IEnumerable<string> args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? key = null;
        foreach (var arg in args)
        {
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                key = arg[2..];
                result[key] = "true";
            }
            else if (key is not null)
            {
                result[key] = arg;
                key = null;
            }
        }

        return result;
    }
}
