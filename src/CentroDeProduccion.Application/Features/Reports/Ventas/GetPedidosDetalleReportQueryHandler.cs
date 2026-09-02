using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Domain.Services;

namespace CentroDeProduccion.Application.Features.Reports.Ventas;

/// <summary>
/// Builds the detailed orders report: one flat row per line of every delivered (Enviado) remito
/// in the range, replicating the daily sheets of the original QUE_MILA Excel. Defaults to the
/// current month (1st to today). Insumo lines carry their principal supplier; finished-product
/// lines show "—".
/// </summary>
public class GetPedidosDetalleReportQueryHandler
{
    private const string SinProveedor = "—";

    private readonly IRemitoRepository _remitoRepository;
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly IInsumoRepository _insumoRepository;
    private readonly IProveedorRepository _proveedorRepository;
    private readonly IUnidadMedidaRepository _unidadMedidaRepository;

    public GetPedidosDetalleReportQueryHandler(
        IRemitoRepository remitoRepository,
        IProductoTerminadoRepository productoTerminadoRepository,
        IInsumoRepository insumoRepository,
        IProveedorRepository proveedorRepository,
        IUnidadMedidaRepository unidadMedidaRepository)
    {
        _remitoRepository = remitoRepository;
        _productoTerminadoRepository = productoTerminadoRepository;
        _insumoRepository = insumoRepository;
        _proveedorRepository = proveedorRepository;
        _unidadMedidaRepository = unidadMedidaRepository;
    }

    public async Task<Result<GetPedidosDetalleReportDto>> HandleAsync(
        GetPedidosDetalleReportQuery query, CancellationToken ct = default)
    {
        var from = query.From ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var to = query.To ?? DateTime.Today;
        if (from > to)
        {
            return Result.Failure<GetPedidosDetalleReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var remitos = await _remitoRepository.GetByFiltersAsync(query.BarId, EstadoRemito.Enviado, from, to, ct);

        var lineas = remitos
            .SelectMany(r => r.Lineas.Select(l => new LineaRemito(r, l)))
            .OrderBy(x => x.Remito.Fecha)
            .ThenBy(x => x.Remito.NumeroRemito)
            .ToList();

        var productosPorId = (await CargarProductosAsync(lineas, ct))
            .ToDictionary(p => p.Id);
        var insumosPorId = (await CargarInsumosAsync(lineas, ct))
            .ToDictionary(i => i.Id);
        // GetByIdsAsync does not include navigation properties: resolve supplier names explicitly.
        var proveedorIds = insumosPorId.Values
            .Where(i => i.ProveedorPrincipalId.HasValue)
            .Select(i => i.ProveedorPrincipalId!.Value)
            .Distinct()
            .ToList();
        var proveedoresPorId = (await _proveedorRepository.GetByIdsAsync(proveedorIds, ct))
            .ToDictionary(p => p.Id, p => p.NombreRazonSocial);
        var unidadesPorId = (await _unidadMedidaRepository.GetAllActiveAsync(ct))
            .ToDictionary(u => u.Id);

        var items = new List<PedidosDetalleReportItem>();
        decimal totalGeneral = 0m;

        foreach (var x in lineas)
        {
            string producto;
            string tipoLinea;
            string unidad;
            string proveedor = SinProveedor;

            if (x.Linea.ProductoTerminadoId.HasValue &&
                productosPorId.TryGetValue(x.Linea.ProductoTerminadoId.Value, out var pt))
            {
                producto = pt.Nombre;
                tipoLinea = "Producto Terminado";
                unidad = unidadesPorId.TryGetValue(pt.UnidadMedidaId, out var uPt) ? uPt.Simbolo : string.Empty;
            }
            else if (x.Linea.InsumoId.HasValue &&
                     insumosPorId.TryGetValue(x.Linea.InsumoId.Value, out var insumo))
            {
                producto = insumo.Nombre;
                tipoLinea = "Insumo";
                unidad = unidadesPorId.TryGetValue(insumo.UnidadConsumoId, out var uIns) ? uIns.Simbolo : string.Empty;
                proveedor = insumo.ProveedorPrincipalId.HasValue &&
                            proveedoresPorId.TryGetValue(insumo.ProveedorPrincipalId.Value, out var nombreProveedor)
                    ? nombreProveedor
                    : SinProveedor;
            }
            else
            {
                continue;
            }

            totalGeneral += x.Linea.Subtotal;

            items.Add(new PedidosDetalleReportItem(
                x.Remito.Fecha,
                x.Remito.NumeroRemito,
                x.Remito.Estado.ToString(),
                x.Remito.Bar.Nombre,
                producto,
                tipoLinea,
                x.Linea.Cantidad,
                unidad,
                Math.Round(x.Linea.PrecioUnitario, 2),
                Math.Round(x.Linea.Subtotal, 2),
                proveedor,
                x.Linea.Lote,
                x.Linea.Observaciones));
        }

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            from,
            to,
            query.BarId.HasValue ? $"Bar: {query.BarId.Value}" : null,
            "pedidos-detalle",
            "Pedidos - detalle");

        return Result.Success(new GetPedidosDetalleReportDto(
            items,
            Math.Round(totalGeneral, 2),
            metadata));
    }

    private sealed record LineaRemito(Domain.Entities.Remito Remito, Domain.Entities.RemitoLinea Linea);

    private async Task<IReadOnlyList<Domain.Entities.ProductoTerminado>> CargarProductosAsync(
        IReadOnlyList<LineaRemito> lineas, CancellationToken ct)
    {
        var ids = lineas
            .Where(x => x.Linea.ProductoTerminadoId.HasValue)
            .Select(x => x.Linea.ProductoTerminadoId!.Value)
            .Distinct()
            .ToList();
        return await _productoTerminadoRepository.GetByIdsAsync(ids, ct);
    }

    private async Task<IReadOnlyList<Domain.Entities.Insumo>> CargarInsumosAsync(
        IReadOnlyList<LineaRemito> lineas, CancellationToken ct)
    {
        var ids = lineas
            .Where(x => x.Linea.InsumoId.HasValue)
            .Select(x => x.Linea.InsumoId!.Value)
            .Distinct()
            .ToList();
        return await _insumoRepository.GetByIdsAsync(ids, ct);
    }
}
