using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Authorization;
using CentroDeProduccion.Application.Features.Reports.Costos;
using CentroDeProduccion.Application.Features.Reports.Compras;
using CentroDeProduccion.Application.Features.Reports.Produccion;
using CentroDeProduccion.Application.Features.Reports.Stock;
using CentroDeProduccion.Application.Features.Reports.Ventas;
using CentroDeProduccion.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly GetProduccionPeriodoReportQueryHandler _getProduccionPeriodoReportHandler;
    private readonly GetProduccionProductoReportQueryHandler _getProduccionProductoReportHandler;
    private readonly GetStockInsumosValoradoReportQueryHandler _getStockInsumosValoradoReportHandler;
    private readonly GetStockInsumosBajoMinimoReportQueryHandler _getStockInsumosBajoMinimoReportHandler;
    private readonly GetStockInsumosMovimientosReportQueryHandler _getStockInsumosMovimientosReportHandler;
    private readonly GetStockPTValoradoReportQueryHandler _getStockPTValoradoReportHandler;
    private readonly GetStockPTProximosAVencerReportQueryHandler _getStockPTProximosAVencerReportHandler;
    private readonly GetStockPTMovimientosReportQueryHandler _getStockPTMovimientosReportHandler;
    private readonly GetComprasPorProveedorReportQueryHandler _getComprasPorProveedorReportHandler;
    private readonly GetEvolucionPreciosReportQueryHandler _getEvolucionPreciosReportHandler;
    private readonly GetCtaCteProveedorReportQueryHandler _getCtaCteProveedorReportHandler;
    private readonly GetResumenProveedoresReportQueryHandler _getResumenProveedoresReportHandler;
    private readonly GetVentasPorBarReportQueryHandler _getVentasPorBarReportHandler;
    private readonly GetVentasPeriodoReportQueryHandler _getVentasPeriodoReportHandler;
    private readonly GetDevolucionesReportQueryHandler _getDevolucionesReportHandler;
    private readonly GetCtaCteBarReportQueryHandler _getCtaCteBarReportHandler;
    private readonly GetCostoProductoReportQueryHandler _getCostoProductoReportHandler;
    private readonly GetRentabilidadProductoReportQueryHandler _getRentabilidadProductoReportHandler;
    private readonly GetRentabilidadBarReportQueryHandler _getRentabilidadBarReportHandler;
    private readonly GetPlanillaCostosReportQueryHandler _getPlanillaCostosReportHandler;
    private readonly GetPedidosDetalleReportQueryHandler _getPedidosDetalleReportHandler;
    private readonly GetMatrizSemanalReportQueryHandler _getMatrizSemanalReportHandler;

    public ReportsController(
        GetProduccionPeriodoReportQueryHandler getProduccionPeriodoReportHandler,
        GetProduccionProductoReportQueryHandler getProduccionProductoReportHandler,
        GetStockInsumosValoradoReportQueryHandler getStockInsumosValoradoReportHandler,
        GetStockInsumosBajoMinimoReportQueryHandler getStockInsumosBajoMinimoReportHandler,
        GetStockInsumosMovimientosReportQueryHandler getStockInsumosMovimientosReportHandler,
        GetStockPTValoradoReportQueryHandler getStockPTValoradoReportHandler,
        GetStockPTProximosAVencerReportQueryHandler getStockPTProximosAVencerReportHandler,
        GetStockPTMovimientosReportQueryHandler getStockPTMovimientosReportHandler,
        GetComprasPorProveedorReportQueryHandler getComprasPorProveedorReportHandler,
        GetEvolucionPreciosReportQueryHandler getEvolucionPreciosReportHandler,
        GetCtaCteProveedorReportQueryHandler getCtaCteProveedorReportHandler,
        GetResumenProveedoresReportQueryHandler getResumenProveedoresReportHandler,
        GetVentasPorBarReportQueryHandler getVentasPorBarReportHandler,
        GetVentasPeriodoReportQueryHandler getVentasPeriodoReportHandler,
        GetDevolucionesReportQueryHandler getDevolucionesReportHandler,
        GetCtaCteBarReportQueryHandler getCtaCteBarReportHandler,
        GetCostoProductoReportQueryHandler getCostoProductoReportHandler,
        GetRentabilidadProductoReportQueryHandler getRentabilidadProductoReportHandler,
        GetRentabilidadBarReportQueryHandler getRentabilidadBarReportHandler,
        GetPlanillaCostosReportQueryHandler getPlanillaCostosReportHandler,
        GetPedidosDetalleReportQueryHandler getPedidosDetalleReportHandler,
        GetMatrizSemanalReportQueryHandler getMatrizSemanalReportHandler)
    {
        _getProduccionPeriodoReportHandler = getProduccionPeriodoReportHandler;
        _getProduccionProductoReportHandler = getProduccionProductoReportHandler;
        _getStockInsumosValoradoReportHandler = getStockInsumosValoradoReportHandler;
        _getStockInsumosBajoMinimoReportHandler = getStockInsumosBajoMinimoReportHandler;
        _getStockInsumosMovimientosReportHandler = getStockInsumosMovimientosReportHandler;
        _getStockPTValoradoReportHandler = getStockPTValoradoReportHandler;
        _getStockPTProximosAVencerReportHandler = getStockPTProximosAVencerReportHandler;
        _getStockPTMovimientosReportHandler = getStockPTMovimientosReportHandler;
        _getComprasPorProveedorReportHandler = getComprasPorProveedorReportHandler;
        _getEvolucionPreciosReportHandler = getEvolucionPreciosReportHandler;
        _getCtaCteProveedorReportHandler = getCtaCteProveedorReportHandler;
        _getResumenProveedoresReportHandler = getResumenProveedoresReportHandler;
        _getVentasPorBarReportHandler = getVentasPorBarReportHandler;
        _getVentasPeriodoReportHandler = getVentasPeriodoReportHandler;
        _getDevolucionesReportHandler = getDevolucionesReportHandler;
        _getCtaCteBarReportHandler = getCtaCteBarReportHandler;
        _getCostoProductoReportHandler = getCostoProductoReportHandler;
        _getRentabilidadProductoReportHandler = getRentabilidadProductoReportHandler;
        _getRentabilidadBarReportHandler = getRentabilidadBarReportHandler;
        _getPlanillaCostosReportHandler = getPlanillaCostosReportHandler;
        _getPedidosDetalleReportHandler = getPedidosDetalleReportHandler;
        _getMatrizSemanalReportHandler = getMatrizSemanalReportHandler;
    }

    // ── Produccion ──

    [HttpGet("produccion/periodo")]
    [Authorize(Policy = AuthorizationPolicies.CanViewProduccion)]
    public async Task<IActionResult> GetProduccionPeriodo(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string agrupacion = "dia",
        CancellationToken cancellationToken = default)
    {
        var result = await _getProduccionPeriodoReportHandler.HandleAsync(
            new GetProduccionPeriodoReportQuery(from, to, agrupacion), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("produccion/producto")]
    [Authorize(Policy = AuthorizationPolicies.CanViewProduccion)]
    public async Task<IActionResult> GetProduccionProducto(
        [FromQuery] Guid? recetaId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await _getProduccionProductoReportHandler.HandleAsync(
            new GetProduccionProductoReportQuery(recetaId, from, to), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    // ── Stock ──

    [HttpGet("stock/insumos/valorado")]
    [Authorize(Policy = AuthorizationPolicies.CanViewStock)]
    public async Task<IActionResult> GetStockInsumosValorado(CancellationToken cancellationToken = default)
    {
        var result = await _getStockInsumosValoradoReportHandler.HandleAsync(
            new GetStockInsumosValoradoReportQuery(), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("stock/insumos/bajo-minimo")]
    [Authorize(Policy = AuthorizationPolicies.CanViewStock)]
    public async Task<IActionResult> GetStockInsumosBajoMinimo(CancellationToken cancellationToken = default)
    {
        var result = await _getStockInsumosBajoMinimoReportHandler.HandleAsync(
            new GetStockInsumosBajoMinimoReportQuery(), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("stock/insumos/movimientos")]
    [Authorize(Policy = AuthorizationPolicies.CanViewStock)]
    public async Task<IActionResult> GetStockInsumosMovimientos(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] TipoMovimientoStock? tipo,
        CancellationToken cancellationToken = default)
    {
        var result = await _getStockInsumosMovimientosReportHandler.HandleAsync(
            new GetStockInsumosMovimientosReportQuery(from, to, tipo), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("stock/pt/valorado")]
    [Authorize(Policy = AuthorizationPolicies.CanViewStock)]
    public async Task<IActionResult> GetStockPTValorado(CancellationToken cancellationToken = default)
    {
        var result = await _getStockPTValoradoReportHandler.HandleAsync(
            new GetStockPTValoradoReportQuery(), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("stock/pt/proximos-vencer")]
    [Authorize(Policy = AuthorizationPolicies.CanViewStock)]
    public async Task<IActionResult> GetStockPTProximosAVencer(CancellationToken cancellationToken = default)
    {
        var result = await _getStockPTProximosAVencerReportHandler.HandleAsync(
            new GetStockPTProximosAVencerReportQuery(), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("stock/pt/movimientos")]
    [Authorize(Policy = AuthorizationPolicies.CanViewStock)]
    public async Task<IActionResult> GetStockPTMovimientos(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? productoTerminadoId,
        CancellationToken cancellationToken = default)
    {
        var result = await _getStockPTMovimientosReportHandler.HandleAsync(
            new GetStockPTMovimientosReportQuery(from, to, productoTerminadoId), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    // ── Compras ──

    [HttpGet("compras/proveedor")]
    [Authorize(Policy = AuthorizationPolicies.CanViewCompras)]
    public async Task<IActionResult> GetComprasPorProveedor(
        [FromQuery] Guid? proveedorId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await _getComprasPorProveedorReportHandler.HandleAsync(
            new GetComprasPorProveedorReportQuery(proveedorId, from, to), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("compras/precios")]
    [Authorize(Policy = AuthorizationPolicies.CanViewCompras)]
    public async Task<IActionResult> GetEvolucionPrecios(
        [FromQuery] Guid? insumoId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await _getEvolucionPreciosReportHandler.HandleAsync(
            new GetEvolucionPreciosReportQuery(insumoId, from, to), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("compras/proveedores/resumen")]
    [Authorize(Policy = AuthorizationPolicies.CanViewCompras)]
    public async Task<IActionResult> GetResumenProveedores(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await _getResumenProveedoresReportHandler.HandleAsync(
            new GetResumenProveedoresReportQuery(from, to), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("compras/cta-cte/proveedor")]
    [Authorize(Policy = AuthorizationPolicies.CanViewCtaCteProveedor)]
    public async Task<IActionResult> GetCtaCteProveedor(
        [FromQuery] Guid proveedorId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await _getCtaCteProveedorReportHandler.HandleAsync(
            new GetCtaCteProveedorReportQuery(proveedorId, from, to), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    // ── Ventas ──

    [HttpGet("ventas/bar")]
    [Authorize(Policy = AuthorizationPolicies.CanViewVentas)]
    public async Task<IActionResult> GetVentasPorBar(
        [FromQuery] Guid? barId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await _getVentasPorBarReportHandler.HandleAsync(
            new GetVentasPorBarReportQuery(barId, from, to), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("ventas/periodo")]
    [Authorize(Policy = AuthorizationPolicies.CanViewVentas)]
    public async Task<IActionResult> GetVentasPeriodo(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string agrupacion = "dia",
        CancellationToken cancellationToken = default)
    {
        var result = await _getVentasPeriodoReportHandler.HandleAsync(
            new GetVentasPeriodoReportQuery(from, to, agrupacion), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("ventas/devoluciones")]
    [Authorize(Policy = AuthorizationPolicies.CanViewVentas)]
    public async Task<IActionResult> GetDevoluciones(
        [FromQuery] Guid? barId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await _getDevolucionesReportHandler.HandleAsync(
            new GetDevolucionesReportQuery(barId, from, to), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("ventas/cta-cte/bar")]
    [Authorize(Policy = AuthorizationPolicies.CanViewCtaCteBar)]
    public async Task<IActionResult> GetCtaCteBar(
        [FromQuery] Guid barId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await _getCtaCteBarReportHandler.HandleAsync(
            new GetCtaCteBarReportQuery(barId, from, to), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    // ── Costos y Rentabilidad (Admin only) ──

    [HttpGet("costos/producto")]
    [Authorize(Policy = AuthorizationPolicies.CanViewCostos)]
    public async Task<IActionResult> GetCostoProducto(
        [FromQuery] Guid? productoId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await _getCostoProductoReportHandler.HandleAsync(
            new GetCostoProductoReportQuery(productoId, from, to), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("rentabilidad/producto")]
    [Authorize(Policy = AuthorizationPolicies.CanViewRentabilidad)]
    public async Task<IActionResult> GetRentabilidadProducto(
        [FromQuery] Guid? productoId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await _getRentabilidadProductoReportHandler.HandleAsync(
            new GetRentabilidadProductoReportQuery(productoId, from, to), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("rentabilidad/bar")]
    [Authorize(Policy = AuthorizationPolicies.CanViewRentabilidad)]
    public async Task<IActionResult> GetRentabilidadBar(
        [FromQuery] Guid? barId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await _getRentabilidadBarReportHandler.HandleAsync(
            new GetRentabilidadBarReportQuery(barId, from, to), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("planilla-costos")]
    [Authorize(Policy = AuthorizationPolicies.CanViewCostos)]
    public async Task<IActionResult> GetPlanillaCostos(
        [FromQuery] Guid recetaId,
        CancellationToken cancellationToken = default)
    {
        var result = await _getPlanillaCostosReportHandler.HandleAsync(
            new GetPlanillaCostosReportQuery(recetaId), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("pedidos/detalle")]
    [Authorize(Policy = AuthorizationPolicies.CanViewVentas)]
    public async Task<IActionResult> GetPedidosDetalle(
        [FromQuery] Guid? barId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        CancellationToken cancellationToken = default)
    {
        var result = await _getPedidosDetalleReportHandler.HandleAsync(
            new GetPedidosDetalleReportQuery(barId, from, to), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("pedidos/matriz")]
    [Authorize(Policy = AuthorizationPolicies.CanViewVentas)]
    public async Task<IActionResult> GetMatrizSemanal(
        [FromQuery] Guid? barId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken = default)
    {
        var result = await _getMatrizSemanalReportHandler.HandleAsync(
            new GetMatrizSemanalReportQuery(barId, from, to), cancellationToken);
        if (result.IsSuccess) Response.SetNoCache();
        return result.ToActionResult(this);
    }
}
