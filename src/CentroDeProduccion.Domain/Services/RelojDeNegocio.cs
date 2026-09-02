namespace CentroDeProduccion.Domain.Services;

/// <summary>
/// Single source of truth for business wall-clock time. The company operates in
/// Argentina (UTC-3), so every stored business date must use Argentina local
/// time to keep "today" filters and reports consistent.
/// </summary>
public static class RelojDeNegocio
{
    private static readonly TimeZoneInfo Zona = Resolve();

    public static DateTime Ahora => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona);

    public static DateTime Hoy => Ahora.Date;

    private static TimeZoneInfo Resolve()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Argentina Standard Time"); }
        catch { return TimeZoneInfo.Local; } // fallback si el SO no tiene la tz
    }
}
