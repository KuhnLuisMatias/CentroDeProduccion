namespace CentroDeProduccion.Application.Features.Produccion.Commands.ConfirmProduccion;

public sealed record ConfirmProduccionCommand(
    Guid ProduccionId,
    decimal CantidadProducida,
    byte[] RowVersion);

public sealed record ConfirmProduccionResponse(
    Guid ProduccionId,
    Guid ProductoTerminadoId,
    string Lote,
    Domain.Enums.EstadoProduccion Estado);
