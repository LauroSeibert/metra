using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Erp.Infrastructure.Persistence;

/// <summary>
/// Datas de calendário (vencimento, baixa) em <see cref="DateOnly"/>, gravadas em colunas DATE. O provider da
/// Oracle lê DATE como <see cref="DateTime"/> e falha ao entregar <see cref="DateOnly"/>; o conversor faz a ponte.
/// </summary>
internal sealed class DateOnlyConverter() : ValueConverter<DateOnly, DateTime>(
    date => date.ToDateTime(TimeOnly.MinValue),
    value => DateOnly.FromDateTime(value));
