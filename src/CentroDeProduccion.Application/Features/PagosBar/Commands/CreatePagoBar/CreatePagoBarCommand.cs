using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.PagosBar.Commands.CreatePagoBar;

public sealed record PagoBarMetodoCommand(
    MetodoPago Tipo,
    decimal Monto,
    string? Referencia);

public sealed record PagoBarItemCommand(
    Guid RemitoId,
    decimal MontoAplicado);

public sealed record CreatePagoBarCommand(
    Guid BarId,
    DateTime? FechaPago,
    decimal MontoTotal,
    string? Observaciones,
    IReadOnlyList<PagoBarMetodoCommand> Metodos,
    IReadOnlyList<PagoBarItemCommand> Items);