using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.CuentaCorrienteBar.Commands.RegisterCompensacion;

public sealed record RegisterCompensacionCommand(
    Guid BarId,
    decimal Monto,
    string? Referencia,
    DateTime? Fecha);

public sealed record RegisterCompensacionResponse(
    Guid Id,
    Guid BarId,
    TipoMovimientoCtaCteBar TipoMovimiento,
    decimal Monto,
    DateTime Fecha,
    string? Referencia);