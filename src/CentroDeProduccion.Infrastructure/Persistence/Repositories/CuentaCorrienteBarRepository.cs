using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Domain.Entities;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Infrastructure.Persistence.Repositories;

public class CuentaCorrienteBarRepository : ICuentaCorrienteBarRepository
{
    private readonly AppDbContext _context;

    public CuentaCorrienteBarRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(CuentaCorrienteBar movimiento, CancellationToken cancellationToken = default)
        => await _context.Set<CuentaCorrienteBar>().AddAsync(movimiento, cancellationToken);

    public async Task<IReadOnlyList<CuentaCorrienteBar>> GetByBarAsync(
        Guid barId,
        TipoMovimientoCtaCteBar? tipo,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        CancellationToken cancellationToken = default)
        => await _context.Set<CuentaCorrienteBar>()
            .Where(cc =>
                cc.BarId == barId &&
                (!tipo.HasValue || cc.TipoMovimiento == tipo.Value) &&
                (!fechaDesde.HasValue || cc.Fecha >= fechaDesde.Value) &&
                (!fechaHasta.HasValue || cc.Fecha < fechaHasta.Value.Date.AddDays(1)))
            .OrderBy(cc => cc.Fecha)
            .ThenBy(cc => cc.FechaCreacion)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<decimal> GetSaldoAsync(Guid barId, CancellationToken cancellationToken = default)
        => await _context.Set<CuentaCorrienteBar>()
            .Where(cc => cc.BarId == barId)
            .SumAsync(cc => (decimal?)cc.Monto, cancellationToken) ?? 0m;

    public async Task<decimal> GetDevolucionTotalByRemitoAsync(Guid remitoId, CancellationToken cancellationToken = default)
        => await _context.Set<CuentaCorrienteBar>()
            .Where(cc => cc.RemitoId == remitoId && cc.TipoMovimiento == TipoMovimientoCtaCteBar.Devolucion)
            .SumAsync(cc => (decimal?)cc.Monto, cancellationToken) ?? 0m;

    public async Task<decimal> GetDeudaTotalAsync(CancellationToken cancellationToken = default)
    {
        // Net balance per bar first (remitos minus devoluciones/pagos), then sum only the
        // positive balances: what is actually owed. A bar with negative saldo must not offset
        // what others still owe.
        var saldos = await _context.Set<CuentaCorrienteBar>()
            .GroupBy(cc => cc.BarId)
            .Select(g => new { Saldo = g.Sum(cc => (decimal?)cc.Monto) ?? 0m })
            .ToListAsync(cancellationToken);

        return saldos.Where(x => x.Saldo > 0m).Sum(x => x.Saldo);
    }
}
