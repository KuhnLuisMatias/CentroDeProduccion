using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Remitos.Queries.GetRemitoList;

public sealed record GetRemitoListQuery(
    Guid? BarId,
    EstadoRemito? Estado,
    DateTime? FechaDesde,
    DateTime? FechaHasta);