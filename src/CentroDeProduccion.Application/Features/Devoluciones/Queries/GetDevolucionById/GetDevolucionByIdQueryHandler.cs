using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Devoluciones.Queries;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Devoluciones.Queries.GetDevolucionById;

public class GetDevolucionByIdQueryHandler
{
    private readonly IDevolucionRepository _devolucionRepository;

    public GetDevolucionByIdQueryHandler(IDevolucionRepository devolucionRepository)
    {
        _devolucionRepository = devolucionRepository;
    }

    public async Task<Result<DevolucionResponse>> HandleAsync(GetDevolucionByIdQuery query, CancellationToken cancellationToken = default)
    {
        var devolucion = await _devolucionRepository.GetByIdWithLineasAsync(query.Id, cancellationToken);
        if (devolucion == null)
        {
            return Result.Failure<DevolucionResponse>(Error.NotFound("DEVOLUCION_NOT_FOUND", "Devolución no encontrada"));
        }

        return Result.Success(Map(devolucion));
    }

    internal static DevolucionResponse Map(Domain.Entities.Devolucion devolucion) => new(
        devolucion.Id,
        devolucion.Numero,
        devolucion.RemitoId,
        devolucion.Remito?.NumeroRemito ?? 0,
        devolucion.Fecha,
        devolucion.Observaciones,
        devolucion.RecibidoPor,
        devolucion.Remito?.BarId ?? Guid.Empty,
        devolucion.Remito?.Bar?.Nombre ?? string.Empty,
        // Total acreditado: solo las líneas que reingresaron a stock generan crédito.
        devolucion.Lineas
            .Where(l => l.Destino == DestinoDevolucion.ReingresoStock)
            .Sum(l => l.Cantidad * PrecioUnitarioOriginal(devolucion, l)),
        devolucion.Lineas
            .Select(l => new DevolucionLineaResponse(
                l.Id,
                l.ProductoTerminadoId.HasValue ? TipoLineaRemito.ProductoTerminado : TipoLineaRemito.Insumo,
                l.ProductoTerminadoId,
                l.ProductoTerminado?.Nombre ?? string.Empty,
                l.InsumoId,
                l.Insumo?.Nombre ?? string.Empty,
                l.Cantidad,
                l.Lote,
                l.Destino,
                PrecioUnitarioOriginal(devolucion, l),
                l.Cantidad * PrecioUnitarioOriginal(devolucion, l)))
            .ToList());

    internal static DevolucionListItemResponse MapListItem(Domain.Entities.Devolucion devolucion) => new(
        devolucion.Id,
        devolucion.Numero,
        devolucion.RemitoId,
        devolucion.Remito?.NumeroRemito ?? 0,
        devolucion.Remito?.BarId ?? Guid.Empty,
        devolucion.Remito?.Bar?.Nombre ?? string.Empty,
        devolucion.Fecha,
        devolucion.Lineas
            .Where(l => l.Destino == DestinoDevolucion.ReingresoStock)
            .Sum(l => l.Cantidad * PrecioUnitarioOriginal(devolucion, l)));

    private static decimal PrecioUnitarioOriginal(Domain.Entities.Devolucion devolucion, Domain.Entities.DevolucionLinea linea)
        => devolucion.Remito?.Lineas
            .FirstOrDefault(l =>
                linea.ProductoTerminadoId.HasValue
                    ? l.TipoLinea == TipoLineaRemito.ProductoTerminado && l.ProductoTerminadoId == linea.ProductoTerminadoId
                    : l.TipoLinea == TipoLineaRemito.Insumo && l.InsumoId == linea.InsumoId)
            ?.PrecioUnitario ?? 0m;
}