using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Inventario;

namespace CentroDeProduccion.Application.Features.Inventario.Queries.GetInventarioSesionList;

public class GetInventarioSesionListQueryHandler
{
    private readonly IInventarioSesionRepository _inventarioSesionRepository;

    public GetInventarioSesionListQueryHandler(IInventarioSesionRepository inventarioSesionRepository)
    {
        _inventarioSesionRepository = inventarioSesionRepository;
    }

    public async Task<Result<IReadOnlyList<GetInventarioSesionListResponse>>> HandleAsync(
        GetInventarioSesionListQuery query, CancellationToken cancellationToken = default)
    {
        var sesiones = await _inventarioSesionRepository.GetAllAsync(cancellationToken);

        var response = sesiones
            .Where(s => query.Estado == null || s.Estado == query.Estado)
            .Where(s => query.Tipo == null || s.TipoInventario == query.Tipo)
            .Where(s => query.Desde == null || s.Fecha >= query.Desde.Value)
            .Where(s => query.Hasta == null || s.Fecha <= query.Hasta.Value)
            .OrderByDescending(s => s.Fecha)
            .Select(s => new GetInventarioSesionListResponse(
                s.Id,
                s.Fecha,
                s.TipoInventario,
                s.Estado,
                s.Conteos.Count,
                s.Conteos.Sum(c => Math.Abs(c.Diferencia)),
                s.RowVersion))
            .ToList();

        return Result.Success<IReadOnlyList<GetInventarioSesionListResponse>>(response);
    }
}
