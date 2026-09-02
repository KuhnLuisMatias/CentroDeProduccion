using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.CuentaCorriente.Commands.RegisterNotaDebito;

public sealed record RegisterNotaDebitoCommand(
    Guid ProveedorId,
    decimal Monto,
    string? Referencia);

public sealed record RegisterNotaDebitoResponse(
    Guid Id,
    Guid ProveedorId,
    TipoMovimientoCtaCte TipoMovimiento,
    decimal Monto,
    DateTime Fecha,
    string? Referencia);