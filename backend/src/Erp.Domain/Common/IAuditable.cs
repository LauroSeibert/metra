namespace Erp.Domain.Common;

/// <summary>
/// Registro com datas de criação e alteração e versão de concorrência, preenchidas pelo ErpDbContext.
/// <see cref="Entity"/> implementa; tipos que herdam de outra base (usuário do Identity) também.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAt { get; }

    DateTime UpdatedAt { get; }

    int Version { get; }
}
