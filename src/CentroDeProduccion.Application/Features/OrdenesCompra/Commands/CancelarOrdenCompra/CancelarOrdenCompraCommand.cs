namespace CentroDeProduccion.Application.Features.OrdenesCompra.Commands.CancelarOrdenCompra;

public sealed record CancelarOrdenCompraCommand(Guid OrdenCompraId);

public sealed record CancelarOrdenCompraResponse(
    Guid OrdenCompraId,
    int Numero,
    Domain.Enums.EstadoOrdenCompra Estado);