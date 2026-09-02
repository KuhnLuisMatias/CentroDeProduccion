using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.CuentaCorriente.Commands.RegisterNotaCredito;

public sealed record RegisterNotaCreditoCommand(
    Guid ProveedorId,
    decimal Monto,
    string? Referencia);

public sealed record RegisterNotaCreditoResponse(
    Guid Id,
    Guid ProveedorId,
    TipoMovimientoCtaCte TipoMovimiento,
    decimal Monto,
    DateTime Fecha,
    string? Referencia);