using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Remitos.Queries;

namespace CentroDeProduccion.Application.Features.Remitos.Queries.GetRemitoById;

public class GetRemitoByIdQueryHandler
{
    private readonly IRemitoRepository _remitoRepository;

    public GetRemitoByIdQueryHandler(IRemitoRepository remitoRepository)
    {
        _remitoRepository = remitoRepository;
    }

    public async Task<Result<RemitoResponse>> HandleAsync(GetRemitoByIdQuery query, CancellationToken cancellationToken = default)
    {
        var remito = await _remitoRepository.GetByIdWithLineasAsync(query.Id, cancellationToken);
        if (remito == null)
        {
            return Result.Failure<RemitoResponse>(Error.NotFound("REMITO_NOT_FOUND", "Remito no encontrado"));
        }

        return Result.Success(Map(remito));
    }

    internal static RemitoResponse Map(Domain.Entities.Remito remito) => new(
        remito.Id,
        remito.NumeroRemito,
        remito.Fecha,
        remito.BarId,
        remito.Bar?.Nombre ?? string.Empty,
        remito.Bar?.Direccion ?? string.Empty,
        remito.Estado,
        remito.Observaciones,
        remito.EntregadoPor,
        remito.RecibidoPor,
        remito.FechaEnvio,
        remito.Lineas.Sum(l => l.Subtotal),
        remito.Lineas
            .Select(l => new RemitoLineaResponse(
                l.Id,
                l.TipoLinea,
                l.ProductoTerminadoId,
                l.ProductoTerminado?.Nombre ?? string.Empty,
                l.InsumoId,
                l.Insumo?.Nombre ?? string.Empty,
                l.Cantidad,
                l.PrecioUnitario,
                l.Subtotal,
                l.Lote,
                l.Observaciones))
            .ToList(),
        remito.RowVersion);
}