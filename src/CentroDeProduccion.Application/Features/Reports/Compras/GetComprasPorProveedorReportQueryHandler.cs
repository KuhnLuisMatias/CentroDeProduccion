using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Reports.Compras;

/// <summary>
/// Builds the purchases-by-supplier report for a date range. Purchase order totals come from the
/// loaded <see cref="OrdenCompra.Items"/>; supplier names from the loaded <see cref="OrdenCompra.Proveedor"/>.
/// </summary>
public class GetComprasPorProveedorReportQueryHandler
{
    private readonly IOrdenCompraRepository _ordenCompraRepository;

    public GetComprasPorProveedorReportQueryHandler(IOrdenCompraRepository ordenCompraRepository)
    {
        _ordenCompraRepository = ordenCompraRepository;
    }

    public async Task<Result<GetComprasPorProveedorReportDto>> HandleAsync(
        GetComprasPorProveedorReportQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var from = query.From ?? today.AddDays(-30);
        var to = query.To ?? today;
        if (from > to)
        {
            return Result.Failure<GetComprasPorProveedorReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var ordenes = await _ordenCompraRepository.GetByFiltersAsync(query.ProveedorId, null, from, to, ct);

        var items = ordenes
            .GroupBy(oc => new { oc.ProveedorId, ProveedorNombre = oc.Proveedor?.NombreRazonSocial ?? string.Empty })
            .Select(g => new ComprasPorProveedorReportItem(
                g.Key.ProveedorId,
                g.Key.ProveedorNombre,
                g.Count(),
                Math.Round(g.Sum(oc => oc.Items.Sum(i => i.CantidadPedida * i.PrecioUnitario)), 2),
                g.Count(oc => oc.Estado == EstadoOrdenCompra.Enviada),
                g.Count(oc => oc.Estado == EstadoOrdenCompra.Cancelada)))
            .OrderByDescending(i => i.TotalMonto)
            .ToList();

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            from,
            to,
            query.ProveedorId.HasValue ? $"Proveedor: {query.ProveedorId.Value}" : null,
            "compras-por-proveedor",
            "Compras por proveedor");

        return Result.Success(new GetComprasPorProveedorReportDto(items, metadata));
    }
}
