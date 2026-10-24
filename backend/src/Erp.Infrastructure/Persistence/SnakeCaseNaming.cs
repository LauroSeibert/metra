using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Erp.Infrastructure.Persistence;

/// <summary>
/// Convenção do banco: tabelas no singular e colunas, chaves e índices em snake_case
/// (Product → product, CategoryId → category_id, ProductCategory → product_category).
/// </summary>
internal static class SnakeCaseNaming
{
    public static void ApplySnakeCaseNames(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var table = ToSnakeCase(entity.ClrType.Name);
            entity.SetTableName(table);

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.Name));
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(key.IsPrimaryKey()
                    ? $"pk_{table}"
                    : $"ak_{table}_{string.Join('_', key.Properties.Select(p => ToSnakeCase(p.Name)))}");
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var columns = string.Join('_', foreignKey.Properties.Select(p => ToSnakeCase(p.Name)));
                foreignKey.SetConstraintName($"fk_{table}_{columns}");
            }

            foreach (var index in entity.GetIndexes())
            {
                var columns = string.Join('_', index.Properties.Select(p => ToSnakeCase(p.Name)));
                index.SetDatabaseName($"{(index.IsUnique ? "ux" : "ix")}_{table}_{columns}");
            }
        }
    }

    public static string ToSnakeCase(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        var builder = new StringBuilder(name.Length + 8);
        for (var i = 0; i < name.Length; i++)
        {
            var current = name[i];
            if (char.IsUpper(current))
            {
                var previousIsLower = i > 0 && char.IsLower(name[i - 1]);
                var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                if (i > 0 && (previousIsLower || (nextIsLower && char.IsUpper(name[i - 1]))))
                {
                    builder.Append('_');
                }

                builder.Append(char.ToLowerInvariant(current));
            }
            else
            {
                builder.Append(current);
            }
        }

        return builder.ToString();
    }
}
