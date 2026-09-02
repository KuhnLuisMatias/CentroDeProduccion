using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.CuentaCorrienteBar.Commands.RegisterNotaCredito;

public sealed record RegisterNotaCreditoCommand(
    Guid BarId,
    decimal Monto,
    string? Referencia,
    DateTime? Fecha);

public sealed record RegisterNotaCreditoResponse(
    Guid Id,
    Guid BarId,
    TipoMovimientoCtaCteBar TipoMovimiento,
    decimal Monto,
    DateTime Fecha,
    string? Referencia);