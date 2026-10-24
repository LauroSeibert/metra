using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Erp.Infrastructure.Persistence;

/// <summary>Grava em UTC e marca <see cref="DateTimeKind.Utc"/> na leitura.</summary>
internal sealed class UtcDateTimeConverter()
    : ValueConverter<DateTime, DateTime>(
        value => value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime(),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
