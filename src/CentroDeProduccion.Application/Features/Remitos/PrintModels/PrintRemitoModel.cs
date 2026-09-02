namespace CentroDeProduccion.Application.Features.Remitos.PrintModels;

/// <summary>
/// Shared model for the HTML print templates (remito and orden de carga). Placeholders are
/// resolved by property name via <see cref="IPrintTemplateService"/>; LineasHtml is a
/// pre-rendered HTML fragment built by the query handlers.
/// </summary>
public sealed class PrintRemitoModel
{
    public int NumeroRemito { get; set; }
    public string Fecha { get; set; } = string.Empty;
    public string BarNombre { get; set; } = string.Empty;
    public string BarDireccion { get; set; } = string.Empty;
    public string BarTelefono { get; set; } = string.Empty;
    public string BarEncargado { get; set; } = string.Empty;
    public string LineasHtml { get; set; } = string.Empty;
    public string Total { get; set; } = string.Empty;
    public string EntregadoPor { get; set; } = string.Empty;
    public string RecibidoPor { get; set; } = string.Empty;
    public string Chofer { get; set; } = string.Empty;
}
