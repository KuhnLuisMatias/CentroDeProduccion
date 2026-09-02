using CentroDeProduccion.Application.Features.Remitos.Commands.CreateRemito;

namespace CentroDeProduccion.Application.Features.Remitos.Commands.UpdateRemito;

public sealed record UpdateRemitoCommand(
    Guid Id,
    Guid BarId,
    string? Observaciones,
    string? EntregadoPor,
    string? RecibidoPor,
    IReadOnlyList<CreateRemitoLineaCommand> Lineas,
    byte[] RowVersion);