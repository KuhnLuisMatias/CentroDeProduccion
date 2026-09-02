namespace CentroDeProduccion.Application.Features.PagosBar.Commands.CreatePagoBar;

public sealed record CreatePagoBarResponse(
    Guid Id,
    int Numero,
    Guid BarId,
    decimal MontoTotal,
    DateTime FechaPago);