using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Inventario;

namespace CentroDeProduccion.Application.Features.Inventario.Queries.GetInventarioSesionById;

public class GetInventarioSesionByIdQueryHandler
{
    private readonly IInventarioSesionRepository _inventarioSesionRepository;

    public GetInventarioSesionByIdQueryHandler(IInventarioSesionRepository inventarioSesionRepository)
    {
        _inventarioSesionRepository = inventarioSesionRepository;
    }

    public async Task<Result<GetInventarioSesionByIdResponse>> HandleAsync(
        GetInventarioSesionByIdQuery query, CancellationToken cancellationToken = default)
    {
        var session = await _inventarioSesionRepository.GetByIdWithConteosAsync(query.Id, cancellationToken);
        if (session == null)
        {
            return Result.Failure<GetInventarioSesionByIdResponse>(
                Error.NotFound("SESION_NOT_FOUND", "Sesión de inventario no encontrada"));
        }

        var conteos = session.Conteos
            .OrderBy(c => c.Insumo?.Nombre ?? c.ProductoTerminado?.Nombre ?? string.Empty)
            .Select(c => new InventarioConteoResponse(
                c.Id,
                c.InsumoId,
                c.Insumo?.Nombre,
                c.ProductoTerminadoId,
                c.ProductoTerminado?.Nombre,
                c.CantidadSistema,
                c.CantidadContada,
                c.Diferencia,
                c.ConteoOk,
                c.Observaciones))
            .ToList();

        return Result.Success(new GetInventarioSesionByIdResponse(
            session.Id,
            session.TipoInventario,
            session.Fecha,
            session.Estado,
            session.ResponsableId,
            session.Notas,
            session.Conteos.Sum(c => Math.Abs(c.Diferencia)),
            conteos,
            session.RowVersion));
    }
}
