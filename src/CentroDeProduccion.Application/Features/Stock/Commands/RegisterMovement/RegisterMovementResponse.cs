namespace CentroDeProduccion.Application.Features.Stock.Commands.RegisterMovement;

public sealed record RegisterMovementResponse(
    Guid MovimientoId,
    Guid TargetId,
    string TargetNombre,
    decimal StockAnterior,
    decimal CantidadMovimiento,
    decimal StockNuevo,
    DateTime Fecha);
