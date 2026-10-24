namespace Erp.Application.Common;

/// <summary>
/// "Hoje" no fuso da empresa (Brasília), para vencimento e atraso: às 22h de 31/10 em São Paulo já é
/// 01/11 em UTC, e o lançamento que vence dia 31 ainda não está atrasado.
/// </summary>
public sealed class BusinessCalendar(TimeProvider clock)
{
    private static readonly TimeZoneInfo Zone = FindZone();

    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(clock.GetUtcNow(), Zone).DateTime);

    public DateTime UtcNow => clock.GetUtcNow().UtcDateTime;

    /// <summary>Primeiro dia do mês corrente no fuso da empresa.</summary>
    public DateOnly MonthStart => new(Today.Year, Today.Month, 1);

    private static TimeZoneInfo FindZone()
    {
        // IANA no Linux (container); no Windows o .NET converte, mas sem ICU pode falhar, então há reserva.
        foreach (var id in new[] { "America/Sao_Paulo", "E. South America Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone))
            {
                return zone;
            }
        }

        // Sem base de fusos: Brasília não tem horário de verão desde 2019.
        return TimeZoneInfo.CreateCustomTimeZone("BRT", TimeSpan.FromHours(-3), "Brasília", "Brasília");
    }
}
