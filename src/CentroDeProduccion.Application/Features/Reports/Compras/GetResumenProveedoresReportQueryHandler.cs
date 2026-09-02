using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;

namespace CentroDeProduccion.Application.Features.Reports.Compras;

/// <summary>
/// Builds the suppliers summary report: one row per active supplier with purchase-order totals for
/// the range and the current outstanding balance from the supplier current-account.
/// </summary>
public class GetResumenProveedoresReportQueryHandler
{
    private readonly IProveedorRepository _proveedorRepository;
    private readonly IOrdenCompraRepository _ordenCompraRepository;
    private readonly ICuentaCorrienteProveedorRepository _ctaCteRepository;

    public GetResumenProveedoresReportQueryHandler(
        IProveedorRepository proveedorRepository,
        IOrdenCompraRepository ordenCompraRepository,
        ICuentaCorrienteProveedorRepository ctaCteRepository)
    {
        _proveedorRepository = proveedorRepository;
        _ordenCompraRepository = ordenCompraRepository;
        _ctaCteRepository = ctaCteRepository;
    }

    public async Task<Result<GetResumenProveedoresReportDto>> HandleAsync(
        GetResumenProveedoresReportQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var from = query.From ?? today.AddDays(-30);
        var to = query.To ?? today;
        if (from > to)
        {
            return Result.Failure<GetResumenProveedoresReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var proveedores = await _proveedorRepository.GetAllActiveAsync(ct);
        var ordenes = await _ordenCompraRepository.GetByFiltersAsync(null, null, from, to, ct);

        // One grouped query for all balances instead of one GetSaldoAsync per supplier (N+1).
        var saldos = await _ctaCteRepository.GetSaldosPorProveedorAsync(ct);

        var items = new List<ResumenProveedoresReportItem>(proveedores.Count);
        foreach (var proveedor in proveedores)
        {
            var proveedorOrdenes = ordenes.Where(o => o.ProveedorId == proveedor.Id).ToList();
            var saldo = saldos.GetValueOrDefault(proveedor.Id);
            items.Add(new ResumenProveedoresReportItem(
                proveedor.Id,
                proveedor.NombreRazonSocial,
                proveedorOrdenes.Count,
                Math.Round(proveedorOrdenes.Sum(o => o.Items.Sum(i => i.CantidadPedida * i.PrecioUnitario)), 2),
                Math.Round(saldo, 2)));
        }

        items = items.OrderByDescending(i => i.TotalMonto).ToList();

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            from,
            to,
            null,
            "resumen-proveedores",
            "Resumen de proveedores");

        return Result.Success(new GetResumenProveedoresReportDto(items, metadata));
    }
}
