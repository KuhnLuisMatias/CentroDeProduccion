using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.CuentaCorrienteBar.Queries;

public sealed record CuentaCorrienteBarMovimientoResponse(
    Guid Id,
    TipoMovimientoCtaCteBar TipoMovimiento,
    decimal Monto,
    string? Referencia,
    DateTime Fecha,
    decimal SaldoAcumulado,
    Guid? RemitoId,
    Guid? DevolucionId,
    Guid? PagoBarId);