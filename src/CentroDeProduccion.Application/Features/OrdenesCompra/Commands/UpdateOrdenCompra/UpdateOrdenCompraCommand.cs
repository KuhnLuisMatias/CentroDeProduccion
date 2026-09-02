using CentroDeProduccion.Application.Features.OrdenesCompra.Commands.CreateOrdenCompra;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.OrdenesCompra.Commands.UpdateOrdenCompra;

public sealed record UpdateOrdenCompraCommand(
    Guid Id,
    Guid ProveedorId,
    string? Observaciones,
    byte[] RowVersion,
    IReadOnlyList<CreateOrdenCompraItemCommand> Items);