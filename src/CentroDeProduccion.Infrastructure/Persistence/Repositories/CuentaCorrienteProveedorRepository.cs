using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class CuentaCorrienteProveedorRepository : ICuentaCorrienteProveedorRepository
{
    private readonly AppDbContext _context;

    public CuentaCorrienteProveedorRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(CuentaCorrienteProveedor movimiento, CancellationToken cancellationToken = default)
        => await _context.CuentasCorrientesProveedores.AddAsync(movimiento, cancellationToken);

    public async Task<IReadOnlyList<CuentaCorrienteProveedor>> GetByProveedorAsync(
        Guid proveedorId,
        TipoMovimientoCtaCte? tipo,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default)
        => await _context.CuentasCorrientesProveedores
            .Where(cc =>
                cc.ProveedorId == proveedorId &&
                (!tipo.HasValue || cc.TipoMovimiento == tipo.Value) &&
                (!fechaDesde.HasValue || cc.Fecha >= fechaDesde.Value) &&
                (!fechaHasta.HasValue || cc.Fecha < fechaHasta.Value.Date.AddDays(1)))
            .OrderBy(cc => cc.Fecha)
            .ThenBy(cc => cc.FechaCreacion)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetSaldoAsync(Guid proveedorId, CancellationToken cancellationToken = default)
        => await _context.CuentasCorrientesProveedores
            .Where(cc => cc.ProveedorId == proveedorId)
            .SumAsync(cc => (decimal?)cc.Monto, cancellationToken) ?? 0m;

    public async Task<decimal> GetDeudaTotalAsync(CancellationToken cancellationToken = default)
    {
        // Net balance per proveedor first (notas débito/compras minus pagos/notas crédito), then
        // sum only the positive balances: what is actually owed. A negative saldo must not
        // offset what other proveedores still owe.
        var saldos = await _context.CuentasCorrientesProveedores
            .GroupBy(cc => cc.ProveedorId)
            .Select(g => new { Saldo = g.Sum(cc => (decimal?)cc.Monto) ?? 0m })
            .ToListAsync(cancellationToken);

        return saldos.Where(x => x.Saldo > 0m).Sum(x => x.Saldo);
    }

    public async Task<Dictionary<Guid, decimal>> GetSaldosPorProveedorAsync(CancellationToken ct = default)
        => await _context.CuentasCorrientesProveedores
            .GroupBy(cc => cc.ProveedorId)
            .Select(g => new { ProveedorId = g.Key, Saldo = g.Sum(cc => (decimal?)cc.Monto) ?? 0m })
            .ToDictionaryAsync(x => x.ProveedorId, x => x.Saldo, ct);

    /// <returns>The settled amount as a POSITIVE decimal (Σ of the negated CC row amounts).</returns>
    public async Task<decimal> GetPagosAplicadosAFacturaAsync(Guid facturaId, CancellationToken cancellationToken = default)
        => await _context.CuentasCorrientesProveedores
            .Where(cc => cc.PagoProveedorId == facturaId &&
                (cc.TipoMovimiento == TipoMovimientoCtaCte.Pago || cc.Monto < 0))
            .SumAsync(cc => (decimal?)-cc.Monto, cancellationToken) ?? 0m;

    public async Task<Dictionary<Guid, decimal>> GetPagosAplicadosPorFacturaAsync(
        IReadOnlyCollection<Guid> facturaIds, CancellationToken cancellationToken = default)
    {
        if (facturaIds.Count == 0)
            return new Dictionary<Guid, decimal>();

        var saldos = await _context.CuentasCorrientesProveedores
            .Where(cc => cc.PagoProveedorId != null && facturaIds.Contains(cc.PagoProveedorId.Value) &&
                (cc.TipoMovimiento == TipoMovimientoCtaCte.Pago || cc.Monto < 0))
            .GroupBy(cc => cc.PagoProveedorId!.Value)
            .Select(g => new { FacturaId = g.Key, Pagado = g.Sum(cc => (decimal?)-cc.Monto) ?? 0m })
            .ToListAsync(cancellationToken);

        return saldos.ToDictionary(x => x.FacturaId, x => x.Pagado);
    }
}