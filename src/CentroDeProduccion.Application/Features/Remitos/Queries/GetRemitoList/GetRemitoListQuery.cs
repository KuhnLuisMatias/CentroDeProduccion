using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Remitos.Queries.GetRemitoList;

public sealed record GetRemitoListQuery(
    Guid? BarId,
    IReadOnlyList<EstadoRemito>? Estados,
    DateTime? FechaDesde,
    DateTime? FechaHasta);