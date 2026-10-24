namespace Erp.Domain.Common;

/// <summary>
/// Base de toda entidade persistida. Datas e versão são mantidas pela infraestrutura
/// (ErpDbContext) no momento do SaveChanges, nunca pelo código de negócio.
/// </summary>
public abstract class Entity : IAuditable
{
    public long Id { get; private set; }

    /// <summary>Criação em UTC.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Última alteração em UTC.</summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Token de concorrência otimista, incrementado a cada alteração.</summary>
    public int Version { get; private set; }
}
