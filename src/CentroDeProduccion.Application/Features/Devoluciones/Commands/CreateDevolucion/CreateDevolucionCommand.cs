namespace CentroDeProduccion.Application.Features.Devoluciones.Commands.CreateDevolucion;

using CentroDeProduccion.Domain.Enums;

/// <summary>One returned line: either a finished product (<see cref="ProductoTerminadoId"/>)
/// or an insumo (<see cref="InsumoId"/>). Exactly one must be set.</summary>
public sealed record CreateDevolucionLineaCommand(
    Guid? ProductoTerminadoId,
    Guid? InsumoId,
    decimal Cantidad,
    string? Lote,
    DestinoDevolucion Destino = DestinoDevolucion.ReingresoStock);

public sealed record CreateDevolucionCommand(
    Guid RemitoId,
    string? Observaciones,
    string? RecibidoPor,
    IReadOnlyList<CreateDevolucionLineaCommand> Lineas);