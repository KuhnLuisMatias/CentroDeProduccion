namespace CentroDeProduccion.Application.Features.Dashboard.Queries;

/// <summary>
/// A single product's average unit cost used for the dashboard cost analysis.
/// </summary>
public sealed record CostoPromedioItem(Guid ProductoId, string Nombre, decimal CostoUnitario);

/// <summary>
/// The headline KPIs rendered on the dashboard cards.
/// </summary>
public sealed record DashboardKPIsDto(
    int ProduccionDia,
    decimal ProduccionMes,
    int StockInsumosCriticos,
    int StockProductosTerminados,
    int ProductosProximosAVencer,
    decimal VentasDia,
    decimal VentasMes,
    decimal DeudaProveedores,
    decimal DeudaBares,
    IReadOnlyList<CostoPromedioItem> CostoPromedioPorProducto);
