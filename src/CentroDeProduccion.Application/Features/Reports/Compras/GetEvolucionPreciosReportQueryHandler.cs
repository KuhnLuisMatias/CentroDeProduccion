using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Reports.Compras;

/// <summary>
/// Builds the input-price-evolution report from purchase stock movements. Input names come from
/// the loaded <see cref="MovimientoStock.Insumo"/> navigation; supplier names are resolved via the
/// input's principal supplier.
/// </summary>
public class GetEvolucionPreciosReportQueryHandler
{
    private readonly IMovimientoStockRepository _movimientoStockRepository;
    private readonly IProveedorRepository _proveedorRepository;

    public GetEvolucionPreciosReportQueryHandler(
        IMovimientoStockRepository movimientoStockRepository,
        IProveedorRepository proveedorRepository)
    {
        _movimientoStockRepository = movimientoStockRepository;
        _proveedorRepository = proveedorRepository;
    }

    public async Task<Result<GetEvolucionPreciosReportDto>> HandleAsync(
        GetEvolucionPreciosReportQuery query, CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var from = query.From ?? today.AddDays(-30);
        var to = query.To ?? today;
        if (from > to)
        {
            return Result.Failure<GetEvolucionPreciosReportDto>(
                Error.Validation("RANGO_INVALIDO", "La fecha 'desde' no puede ser posterior a 'hasta'."));
        }

        var movimientos = await _movimientoStockRepository.GetByFiltersAsync(
            from, to, insumoId: query.InsumoId, tipo: TipoMovimientoStock.Compra, ct: ct);

        var proveedores = await LoadProveedorNamesAsync(movimientos, ct);

        var items = movimientos
            .Where(m => m.InsumoId.HasValue && m.PrecioUnitario.HasValue)
            .OrderBy(m => m.InsumoId)
            .ThenBy(m => m.Fecha)
            .Select(m => new EvolucionPreciosReportItem(
                m.InsumoId!.Value,
                m.Insumo?.Nombre ?? string.Empty,
                m.Fecha,
                m.PrecioUnitario!.Value,
                ResolveProveedor(m.Insumo?.ProveedorPrincipalId, proveedores)))
            .ToList();

        var metadata = new ReportMetadata(
            RelojDeNegocio.Ahora,
            from,
            to,
            query.InsumoId.HasValue ? $"Insumo: {query.InsumoId.Value}" : null,
            "evolucion-precios",
            "Evolución de precios");

        return Result.Success(new GetEvolucionPreciosReportDto(items, metadata));
    }

    private async Task<Dictionary<Guid, string>> LoadProveedorNamesAsync(
        IEnumerable<MovimientoStock> movimientos, CancellationToken ct)
    {
        var ids = movimientos
            .Select(m => m.Insumo?.ProveedorPrincipalId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        // Single batched query instead of one GetByIdAsync per distinct supplier (N+1).
        var proveedores = await _proveedorRepository.GetByIdsAsync(ids, ct);
        return proveedores.ToDictionary(p => p.Id, p => p.NombreRazonSocial);
    }

    private static string? ResolveProveedor(Guid? proveedorId, IReadOnlyDictionary<Guid, string> proveedores)
        => proveedorId.HasValue && proveedores.TryGetValue(proveedorId.Value, out var nombre)
            ? nombre
            : null;
}
