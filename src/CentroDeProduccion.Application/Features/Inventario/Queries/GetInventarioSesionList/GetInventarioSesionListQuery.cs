using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Inventario.Queries.GetInventarioSesionList;

public sealed record GetInventarioSesionListQuery(
    EstadoInventario? Estado,
    TipoInventario? Tipo,
    DateTime? Desde,
    DateTime? Hasta);
