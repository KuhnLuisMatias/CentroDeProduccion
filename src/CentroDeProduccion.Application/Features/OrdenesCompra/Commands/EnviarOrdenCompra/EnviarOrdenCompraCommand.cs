namespace CentroDeProduccion.Application.Features.OrdenesCompra.Commands.EnviarOrdenCompra;

public sealed record EnviarOrdenCompraCommand(Guid OrdenCompraId);

public sealed record EnviarOrdenCompraResponse(
    Guid OrdenCompraId,
    int Numero,
    Domain.Enums.EstadoOrdenCompra Estado);