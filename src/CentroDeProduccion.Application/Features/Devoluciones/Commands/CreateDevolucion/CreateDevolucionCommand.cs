namespace CentroDeProduccion.Application.Features.Devoluciones.Commands.CreateDevolucion;

public sealed record CreateDevolucionLineaCommand(
    Guid ProductoTerminadoId,
    decimal Cantidad,
    string? Lote);

public sealed record CreateDevolucionCommand(
    Guid RemitoId,
    string? Observaciones,
    string? RecibidoPor,
    IReadOnlyList<CreateDevolucionLineaCommand> Lineas);