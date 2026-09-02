using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.OrdenesCompra.Commands.CreateOrdenCompra;

public sealed record CreateOrdenCompraItemCommand(
    Guid InsumoId,
    decimal CantidadPedida,
    decimal PrecioUnitario);

public sealed record CreateOrdenCompraCommand(
    Guid ProveedorId,
    string? Observaciones,
    IReadOnlyList<CreateOrdenCompraItemCommand> Items);