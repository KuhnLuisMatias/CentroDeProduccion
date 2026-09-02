using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.OrdenesCompra.Queries.GetOrdenCompraList;

public sealed record GetOrdenCompraListQuery(
    Guid? ProveedorId,
    EstadoOrdenCompra? Estado,
    DateTime? FechaDesde,
    DateTime? FechaHasta);