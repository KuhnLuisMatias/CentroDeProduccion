using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.CuentaCorriente.Queries;

public sealed record CuentaCorrienteMovimientoResponse(
    Guid Id,
    TipoMovimientoCtaCte TipoMovimiento,
    decimal Monto,
    DateTime Fecha,
    string? Referencia,
    Guid? OrdenCompraId,
    Guid? PagoProveedorId,
    decimal Saldo);