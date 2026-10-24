namespace Erp.Application.Abstractions;

/// <summary>Usuário da requisição atual. Implementado na Api a partir do token.</summary>
public interface ICurrentUser
{
    /// <summary>Id do usuário autenticado; lança se a requisição for anônima.</summary>
    long Id { get; }

    string Role { get; }
}
