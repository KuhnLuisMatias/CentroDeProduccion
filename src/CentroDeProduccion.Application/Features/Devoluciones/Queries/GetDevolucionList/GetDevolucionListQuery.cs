namespace CentroDeProduccion.Application.Features.Devoluciones.Queries.GetDevolucionList;

public sealed record GetDevolucionListQuery(
    Guid? RemitoId,
    Guid? BarId,
    DateTime? FechaDesde,
    DateTime? FechaHasta);