namespace Erp.Domain.Access;

/// <summary>
/// Perfis de acesso. Admin: tudo, inclusive configurações e usuários. Operator: cadastros, vendas,
/// estoque e financeiro. Viewer: somente consulta.
/// </summary>
public static class Roles
{
    public const string Admin = "Admin";
    public const string Operator = "Operator";
    public const string Viewer = "Viewer";

    public static IReadOnlyList<string> All { get; } = [Admin, Operator, Viewer];

    public static bool IsValid(string? role) => role is not null && All.Contains(role);

    /// <summary>Perfis que podem gravar dados operacionais (cadastros e movimentos).</summary>
    public static bool CanWrite(string role) => role is Admin or Operator;
}
