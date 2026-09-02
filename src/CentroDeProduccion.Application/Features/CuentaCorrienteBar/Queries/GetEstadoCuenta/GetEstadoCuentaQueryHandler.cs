using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.CuentaCorrienteBar.Queries;
using CentroDeProduccion.Domain.Enums;

namespace CentroDeProduccion.Application.Features.CuentaCorrienteBar.Queries.GetEstadoCuenta;

public sealed record GetEstadoCuentaQuery(
    Guid BarId,
    TipoMovimientoCtaCteBar? Tipo,
    DateTime? FechaDesde,
    DateTime? FechaHasta);

/// <summary>
/// Returns the chronological movements for a bar with a running saldo computed after each
/// movement. Saldo is always derived (SUM of all movements up to that point), never stored.
/// </summary>
public class GetEstadoCuentaQueryHandler
{
    private readonly ICuentaCorrienteBarRepository _cuentaCorrienteRepository;
    private readonly IBarRepository _barRepository;

    public GetEstadoCuentaQueryHandler(
        ICuentaCorrienteBarRepository cuentaCorrienteRepository,
        IBarRepository barRepository)
    {
        _cuentaCorrienteRepository = cuentaCorrienteRepository;
        _barRepository = barRepository;
    }

    public async Task<Result<IReadOnlyList<CuentaCorrienteBarMovimientoResponse>>> HandleAsync(
        GetEstadoCuentaQuery query, CancellationToken cancellationToken = default)
    {
        var bar = await _barRepository.GetByIdAsync(query.BarId, cancellationToken);
        if (bar == null)
        {
            return Result.Failure<IReadOnlyList<CuentaCorrienteBarMovimientoResponse>>(
                Error.NotFound("BAR_NOT_FOUND", "Bar no encontrado"));
        }

        var movimientos = await _cuentaCorrienteRepository.GetByBarAsync(
            query.BarId, query.Tipo, query.FechaDesde, query.FechaHasta, cancellationToken);

        var saldo = 0m;
        var response = movimientos.OrderBy(m => m.Fecha).Select(m =>
        {
            saldo += m.Monto;
            return new CuentaCorrienteBarMovimientoResponse(
                m.Id, m.TipoMovimiento, m.Monto, m.Referencia, m.Fecha, saldo, m.RemitoId, m.DevolucionId, m.PagoBarId);
        }).ToList();

        return Result.Success<IReadOnlyList<CuentaCorrienteBarMovimientoResponse>>(response);
    }
}