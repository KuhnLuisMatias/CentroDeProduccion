namespace CentroDeProduccion.Application.Features.Devoluciones.Commands.CreateDevolucion;

public sealed record CreateDevolucionResponse(
    Guid Id,
    int Numero,
    Guid RemitoId,
    decimal Total,
    DateTime Fecha);