using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.CuentaCorriente.Commands.RegistrarPagoProveedor;

public sealed record RegistrarPagoMetodoDto(int Tipo, decimal Monto, string? Referencia);

public sealed record RegistrarPagoProveedorCommand(
    Guid ProveedorId,
    DateTime Fecha,
    Guid? FacturaId,
    string? Observaciones,
    IReadOnlyList<RegistrarPagoMetodoDto> Medios);

public sealed record RegistrarPagoProveedorResponse(
    Guid Id,
    Guid ProveedorId,
    decimal MontoTotal,
    DateTime Fecha,
    Guid? FacturaId,
    decimal? FacturaMontoTotal,
    decimal? FacturaPagado,
    decimal? FacturaPendiente);
