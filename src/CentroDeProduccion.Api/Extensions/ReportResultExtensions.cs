namespace CentroDeProduccion.Api.Extensions;

/// <summary>
/// Shared HTTP helpers for the "Reportes y dashboard" endpoints.
/// </summary>
public static class ReportResultExtensions
{
    /// <summary>
    /// Disables HTTP caching for the response. Dashboard/real-time report data must always be
    /// freshly computed, so neither the browser nor intermediate proxies may serve a stale copy.
    /// </summary>
    public static void SetNoCache(this HttpResponse response)
    {
        response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        response.Headers.Pragma = "no-cache";
        response.Headers.Expires = "0";
    }
}
