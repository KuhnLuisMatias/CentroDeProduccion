using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.Remitos.Commands.CreateRemito;

public sealed record CreateRemitoLineaCommand(
    TipoLineaRemito TipoLinea,
    Guid? ProductoTerminadoId,
    Guid? InsumoId,
    decimal Cantidad,
    string? Lote,
    string? Observaciones);

public sealed record CreateRemitoCommand(
    Guid BarId,
    string? Observaciones,
    string? EntregadoPor,
    string? RecibidoPor,
    IReadOnlyList<CreateRemitoLineaCommand> Lineas);