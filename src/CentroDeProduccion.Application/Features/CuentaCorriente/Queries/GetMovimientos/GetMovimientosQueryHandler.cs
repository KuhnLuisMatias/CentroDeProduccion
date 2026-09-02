using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.CuentaCorriente.Queries;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.CuentaCorriente.Queries.GetMovimientos;

public sealed record GetMovimientosQuery(
    Guid ProveedorId,
    TipoMovimientoCtaCte? Tipo,
    DateTime? FechaDesde,
    DateTime? FechaHasta);

public class GetMovimientosQueryHandler
{
    private readonly ICuentaCorrienteProveedorRepository _cuentaCorrienteRepository;
    private readonly IProveedorRepository _proveedorRepository;

    public GetMovimientosQueryHandler(
        ICuentaCorrienteProveedorRepository cuentaCorrienteRepository,
        IProveedorRepository proveedorRepository)
    {
        _cuentaCorrienteRepository = cuentaCorrienteRepository;
        _proveedorRepository = proveedorRepository;
    }

    public async Task<Result<IReadOnlyList<CuentaCorrienteMovimientoResponse>>> HandleAsync(
        GetMovimientosQuery query, CancellationToken cancellationToken = default)
    {
        var proveedor = await _proveedorRepository.GetByIdAsync(query.ProveedorId, cancellationToken);
        if (proveedor == null)
        {
            return Result.Failure<IReadOnlyList<CuentaCorrienteMovimientoResponse>>(
                Error.NotFound("PROVEEDOR_NOT_FOUND", "Proveedor no encontrado"));
        }

        var movimientos = await _cuentaCorrienteRepository.GetByProveedorAsync(
            query.ProveedorId, query.Tipo, query.FechaDesde, query.FechaHasta, cancellationToken);

        var response = movimientos.Select(m => new CuentaCorrienteMovimientoResponse(
            m.Id, m.TipoMovimiento, m.Monto, m.Fecha, m.Referencia, m.OrdenCompraId, m.PagoProveedorId, 0m)).ToList();

        return Result.Success<IReadOnlyList<CuentaCorrienteMovimientoResponse>>(response);
    }
}