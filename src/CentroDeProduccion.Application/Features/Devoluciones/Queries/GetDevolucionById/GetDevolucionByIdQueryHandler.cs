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
        devolucion.Lineas.Sum(l => l.Cantidad * PrecioUnitarioOriginal(devolucion, l.ProductoTerminadoId)),
        devolucion.Lineas
            .Select(l => new DevolucionLineaResponse(
                l.Id,
                l.ProductoTerminado?.Nombre ?? string.Empty,
                l.Cantidad,
                l.Lote,
                PrecioUnitarioOriginal(devolucion, l.ProductoTerminadoId),
                l.Cantidad * PrecioUnitarioOriginal(devolucion, l.ProductoTerminadoId)))
            .ToList());

    internal static DevolucionListItemResponse MapListItem(Domain.Entities.Devolucion devolucion) => new(
        devolucion.Id,
        devolucion.Numero,
        devolucion.RemitoId,
        devolucion.Remito?.NumeroRemito ?? 0,
        devolucion.Remito?.BarId ?? Guid.Empty,
        devolucion.Remito?.Bar?.Nombre ?? string.Empty,
        devolucion.Fecha,
        devolucion.Lineas.Sum(l => l.Cantidad * PrecioUnitarioOriginal(devolucion, l.ProductoTerminadoId)));

    private static decimal PrecioUnitarioOriginal(Domain.Entities.Devolucion devolucion, Guid productoTerminadoId)
        => devolucion.Remito?.Lineas
            .FirstOrDefault(l => l.TipoLinea == TipoLineaRemito.ProductoTerminado && l.ProductoTerminadoId == productoTerminadoId)
            ?.PrecioUnitario ?? 0m;
}