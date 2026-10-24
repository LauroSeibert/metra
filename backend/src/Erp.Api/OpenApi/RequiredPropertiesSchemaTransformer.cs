using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Erp.Api.OpenApi;

/// <summary>
/// Marca como obrigatória toda propriedade não anulável dos DTOs. Assim o cliente TypeScript
/// gerado tem <c>sku: string</c> em vez de <c>sku?: string</c>, espelhando o C#.
/// </summary>
internal sealed class RequiredPropertiesSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        if (schema.Properties is null || schema.Properties.Count == 0)
        {
            return Task.CompletedTask;
        }

        foreach (var property in context.JsonTypeInfo.Properties)
        {
            if (!property.IsGetNullable && schema.Properties.ContainsKey(property.Name))
            {
                schema.Required ??= new HashSet<string>();
                schema.Required.Add(property.Name);
            }
        }

        return Task.CompletedTask;
    }
}
