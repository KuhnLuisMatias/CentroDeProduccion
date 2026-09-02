using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.CuentaCorrienteBar.Commands.RegisterNotaDebito;

public sealed record RegisterNotaDebitoCommand(
    Guid BarId,
    decimal Monto,
    string? Referencia,
    DateTime? Fecha);

public sealed record RegisterNotaDebitoResponse(
    Guid Id,
    Guid BarId,
    TipoMovimientoCtaCteBar TipoMovimiento,
    decimal Monto,
    DateTime Fecha,
    string? Referencia);