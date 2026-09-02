using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.CuentaCorriente.Queries;

namespace CentroDeProduccion.Application.Features.CuentaCorriente.Queries.GetEstadoCuenta;

public sealed record GetEstadoCuentaQuery(
    Guid ProveedorId,
    DateTime? FechaDesde,
    DateTime? FechaHasta);

/// <summary>
/// Returns the chronological movements for a supplier with a running saldo computed after each
/// movement. Saldo is always derived (SUM of all movements up to that point), never stored.
/// </summary>
public class GetEstadoCuentaQueryHandler
{
    private readonly ICuentaCorrienteProveedorRepository _cuentaCorrienteRepository;
    private readonly IProveedorRepository _proveedorRepository;

    public GetEstadoCuentaQueryHandler(
        ICuentaCorrienteProveedorRepository cuentaCorrienteRepository,
        IProveedorRepository proveedorRepository)
    {
        _cuentaCorrienteRepository = cuentaCorrienteRepository;
        _proveedorRepository = proveedorRepository;
    }

    public async Task<Result<IReadOnlyList<CuentaCorrienteMovimientoResponse>>> HandleAsync(
        GetEstadoCuentaQuery query, CancellationToken cancellationToken = default)
    {
        var proveedor = await _proveedorRepository.GetByIdAsync(query.ProveedorId, cancellationToken);
        if (proveedor == null)
        {
            return Result.Failure<IReadOnlyList<CuentaCorrienteMovimientoResponse>>(
                Error.NotFound("PROVEEDOR_NOT_FOUND", "Proveedor no encontrado"));
        }

        var movimientos = await _cuentaCorrienteRepository.GetByProveedorAsync(
            query.ProveedorId, null, query.FechaDesde, query.FechaHasta, cancellationToken);

        var saldo = 0m;
        var response = movimientos.Select(m =>
        {
            saldo += m.Monto;
            return new CuentaCorrienteMovimientoResponse(
                m.Id, m.TipoMovimiento, m.Monto, m.Fecha, m.Referencia, m.OrdenCompraId, m.PagoProveedorId, saldo);
        }).ToList();

        return Result.Success<IReadOnlyList<CuentaCorrienteMovimientoResponse>>(response);
    }
}