using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Bares.Queries;

namespace CentroDeProduccion.Application.Features.Bares.Queries.GetBarById;

public class GetBarByIdQueryHandler
{
    private readonly IBarRepository _barRepository;

    public GetBarByIdQueryHandler(IBarRepository barRepository)
    {
        _barRepository = barRepository;
    }

    public async Task<Result<BarResponse>> HandleAsync(GetBarByIdQuery query, CancellationToken cancellationToken = default)
    {
        var bar = await _barRepository.GetByIdAsync(query.Id, cancellationToken);
        if (bar == null)
        {
            return Result.Failure<BarResponse>(Error.NotFound("BAR_NOT_FOUND", "Bar no encontrado"));
        }

        return Result.Success(Map(bar));
    }

    internal static BarResponse Map(Domain.Entities.Bar bar) => new(
        bar.Id,
        bar.Nombre,
        bar.Direccion,
        bar.Encargado,
        bar.Telefono,
        bar.HorarioRecepcion,
        bar.MargenReventaPorcentaje,
        bar.Estado,
        bar.FechaCreacion,
        bar.RowVersion);
}