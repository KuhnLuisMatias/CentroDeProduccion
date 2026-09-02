using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Abstractions;
using CentroDeProduccion.Application.Authorization;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Reports.Costos;
using CentroDeProduccion.Application.Features.Reports.Compras;
using CentroDeProduccion.Application.Features.Reports.Produccion;
using CentroDeProduccion.Application.Features.Reports.Stock;
using CentroDeProduccion.Application.Features.Reports.Ventas;
using CentroDeProduccion.Domain.Enums;
using CentroDeProduccion.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize]
public class ExportReportsController : ControllerBase
{
    private readonly GetProduccionPeriodoReportQueryHandler _produccionPeriodo;
    private readonly GetProduccionProductoReportQueryHandler _produccionProducto;
    private readonly GetStockInsumosValoradoReportQueryHandler _stockInsumosValorado;
    private readonly GetStockInsumosBajoMinimoReportQueryHandler _stockInsumosBajoMinimo;
    private readonly GetStockInsumosMovimientosReportQueryHandler _stockInsumosMovimientos;
    private readonly GetStockPTValoradoReportQueryHandler _stockPTValorado;
    private readonly GetStockPTProximosAVencerReportQueryHandler _stockPTProximosAVencer;
    private readonly GetStockPTMovimientosReportQueryHandler _stockPTMovimientos;
    private readonly GetComprasPorProveedorReportQueryHandler _comprasPorProveedor;
    private readonly GetEvolucionPreciosReportQueryHandler _evolucionPrecios;
    private readonly GetCtaCteProveedorReportQueryHandler _ctaCteProveedor;
    private readonly GetResumenProveedoresReportQueryHandler _resumenProveedores;
    private readonly GetVentasPorBarReportQueryHandler _ventasPorBar;
    private readonly GetVentasPeriodoReportQueryHandler _ventasPeriodo;
    private readonly GetDevolucionesReportQueryHandler _devoluciones;
    private readonly GetCtaCteBarReportQueryHandler _ctaCteBar;
    private readonly GetCostoProductoReportQueryHandler _costoProducto;
    private readonly GetRentabilidadProductoReportQueryHandler _rentabilidadProducto;
    private readonly GetRentabilidadBarReportQueryHandler _rentabilidadBar;
    private readonly GetPlanillaCostosReportQueryHandler _planillaCostos;
    private readonly GetPedidosDetalleReportQueryHandler _pedidosDetalle;
    private readonly GetMatrizSemanalReportQueryHandler _matrizSemanal;

    private readonly IExportService _exportService;
    private readonly ExcelExportService _excelExportService;
    private readonly PdfExportService _pdfExportService;
    private readonly IAuthorizationService _authorizationService;

    public ExportReportsController(
        GetProduccionPeriodoReportQueryHandler produccionPeriodo,
        GetProduccionProductoReportQueryHandler produccionProducto,
        GetStockInsumosValoradoReportQueryHandler stockInsumosValorado,
        GetStockInsumosBajoMinimoReportQueryHandler stockInsumosBajoMinimo,
        GetStockInsumosMovimientosReportQueryHandler stockInsumosMovimientos,
        GetStockPTValoradoReportQueryHandler stockPTValorado,
        GetStockPTProximosAVencerReportQueryHandler stockPTProximosAVencer,
        GetStockPTMovimientosReportQueryHandler stockPTMovimientos,
        GetComprasPorProveedorReportQueryHandler comprasPorProveedor,
        GetEvolucionPreciosReportQueryHandler evolucionPrecios,
        GetCtaCteProveedorReportQueryHandler ctaCteProveedor,
        GetResumenProveedoresReportQueryHandler resumenProveedores,
        GetVentasPorBarReportQueryHandler ventasPorBar,
        GetVentasPeriodoReportQueryHandler ventasPeriodo,
        GetDevolucionesReportQueryHandler devoluciones,
        GetCtaCteBarReportQueryHandler ctaCteBar,
        GetCostoProductoReportQueryHandler costoProducto,
        GetRentabilidadProductoReportQueryHandler rentabilidadProducto,
        GetRentabilidadBarReportQueryHandler rentabilidadBar,
        GetPlanillaCostosReportQueryHandler planillaCostos,
        GetPedidosDetalleReportQueryHandler pedidosDetalle,
        GetMatrizSemanalReportQueryHandler matrizSemanal,
        IExportService exportService,
        ExcelExportService excelExportService,
        PdfExportService pdfExportService,
        IAuthorizationService authorizationService)
    {
        _produccionPeriodo = produccionPeriodo;
        _produccionProducto = produccionProducto;
        _stockInsumosValorado = stockInsumosValorado;
        _stockInsumosBajoMinimo = stockInsumosBajoMinimo;
        _stockInsumosMovimientos = stockInsumosMovimientos;
        _stockPTValorado = stockPTValorado;
        _stockPTProximosAVencer = stockPTProximosAVencer;
        _stockPTMovimientos = stockPTMovimientos;
        _comprasPorProveedor = comprasPorProveedor;
        _evolucionPrecios = evolucionPrecios;
        _ctaCteProveedor = ctaCteProveedor;
        _resumenProveedores = resumenProveedores;
        _ventasPorBar = ventasPorBar;
        _ventasPeriodo = ventasPeriodo;
        _devoluciones = devoluciones;
        _ctaCteBar = ctaCteBar;
        _costoProducto = costoProducto;
        _rentabilidadProducto = rentabilidadProducto;
        _rentabilidadBar = rentabilidadBar;
        _planillaCostos = planillaCostos;
        _pedidosDetalle = pedidosDetalle;
        _matrizSemanal = matrizSemanal;
        _exportService = exportService;
        _excelExportService = excelExportService;
        _pdfExportService = pdfExportService;
        _authorizationService = authorizationService;
    }

    [HttpGet("{type}/export/{format}")]
    public async Task<IActionResult> Export(
        string type,
        string format,
        [FromQuery] Guid? proveedorId,
        [FromQuery] Guid? insumoId,
        [FromQuery] Guid? barId,
        [FromQuery] Guid? recetaId,
        [FromQuery] Guid? productoTerminadoId,
        [FromQuery] Guid? productoId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? agrupacion,
        [FromQuery] TipoMovimientoStock? tipo,
        CancellationToken cancellationToken = default)
    {
        var policyName = GetPolicyForType(type);
        if (policyName is null)
        {
            return NotFound();
        }

        var auth = await _authorizationService.AuthorizeAsync(User, null, policyName);
        if (!auth.Succeeded)
        {
            return Forbid();
        }

        ReportTable reportTable;
        try
        {
            reportTable = await BuildReportTableAsync(
                type, proveedorId, insumoId, barId, recetaId,
                productoTerminadoId, productoId, from, to, agrupacion, tipo, cancellationToken);
        }
        catch (NotSupportedException)
        {
            return NotFound();
        }

        if (reportTable is null)
        {
            return NotFound();
        }

        Response.SetNoCache();

        byte[] bytes;
        string contentType;
        string extension;
        switch (format.ToLowerInvariant())
        {
            case "excel":
                bytes = await _excelExportService.ExportToExcelAsync(reportTable, cancellationToken);
                contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                extension = "xlsx";
                break;
            case "pdf":
                bytes = await _pdfExportService.ExportToPdfAsync(reportTable, cancellationToken);
                contentType = "application/pdf";
                extension = "pdf";
                break;
            default:
                return BadRequest();
        }

        var fileName = $"{type}-{DateTime.Today:yyyy-MM-dd}.{extension}";
        return File(bytes, contentType, fileName);
    }

    private string? GetPolicyForType(string type) => type switch
    {
        "produccion-periodo" or "produccion-producto" => AuthorizationPolicies.CanViewProduccion,
        "stock-insumos-valorado" or "stock-insumos-bajo-minimo" or "stock-insumos-movimientos"
            or "stock-pt-valorado" or "stock-pt-proximos-vencer" or "stock-pt-movimientos" => AuthorizationPolicies.CanViewStock,
        "compras-proveedor" or "compras-precios" or "compras-proveedores-resumen" => AuthorizationPolicies.CanViewCompras,
        "compras-cta-cte-proveedor" => AuthorizationPolicies.CanViewCtaCteProveedor,
        "ventas-bar" or "ventas-periodo" or "ventas-devoluciones" => AuthorizationPolicies.CanViewVentas,
        "ventas-cta-cte-bar" => AuthorizationPolicies.CanViewCtaCteBar,
        "costos-producto" => AuthorizationPolicies.CanViewCostos,
        "planilla-costos" => AuthorizationPolicies.CanViewCostos,
        "pedidos-detalle" or "pedidos-matriz" => AuthorizationPolicies.CanViewVentas,
        "rentabilidad-producto" or "rentabilidad-bar" => AuthorizationPolicies.CanViewRentabilidad,
        _ => null
    };

    private async Task<ReportTable?> BuildReportTableAsync(
        string type,
        Guid? proveedorId, Guid? insumoId, Guid? barId, Guid? recetaId,
        Guid? productoTerminadoId, Guid? productoId,
        DateTime? from, DateTime? to, string? agrupacion, TipoMovimientoStock? tipo,
        CancellationToken ct)
    {
        switch (type)
        {
            case "produccion-periodo":
                return ToReportTable(await _produccionPeriodo.HandleAsync(new GetProduccionPeriodoReportQuery(from, to, agrupacion ?? "dia"), ct), d => d.ToReportTable());
            case "produccion-producto":
                return ToReportTable(await _produccionProducto.HandleAsync(new GetProduccionProductoReportQuery(recetaId, from, to), ct), d => d.ToReportTable());
            case "stock-insumos-valorado":
                return ToReportTable(await _stockInsumosValorado.HandleAsync(new GetStockInsumosValoradoReportQuery(), ct), d => d.ToReportTable());
            case "stock-insumos-bajo-minimo":
                return ToReportTable(await _stockInsumosBajoMinimo.HandleAsync(new GetStockInsumosBajoMinimoReportQuery(), ct), d => d.ToReportTable());
            case "stock-insumos-movimientos":
                return ToReportTable(await _stockInsumosMovimientos.HandleAsync(new GetStockInsumosMovimientosReportQuery(from, to, tipo), ct), d => d.ToReportTable());
            case "stock-pt-valorado":
                return ToReportTable(await _stockPTValorado.HandleAsync(new GetStockPTValoradoReportQuery(), ct), d => d.ToReportTable());
            case "stock-pt-proximos-vencer":
                return ToReportTable(await _stockPTProximosAVencer.HandleAsync(new GetStockPTProximosAVencerReportQuery(), ct), d => d.ToReportTable());
            case "stock-pt-movimientos":
                return ToReportTable(await _stockPTMovimientos.HandleAsync(new GetStockPTMovimientosReportQuery(from, to, productoTerminadoId), ct), d => d.ToReportTable());
            case "compras-proveedor":
                return ToReportTable(await _comprasPorProveedor.HandleAsync(new GetComprasPorProveedorReportQuery(proveedorId, from, to), ct), d => d.ToReportTable());
            case "compras-precios":
                return ToReportTable(await _evolucionPrecios.HandleAsync(new GetEvolucionPreciosReportQuery(insumoId, from, to), ct), d => d.ToReportTable());
            case "compras-proveedores-resumen":
                return ToReportTable(await _resumenProveedores.HandleAsync(new GetResumenProveedoresReportQuery(from, to), ct), d => d.ToReportTable());
            case "compras-cta-cte-proveedor":
                return ToReportTable(await _ctaCteProveedor.HandleAsync(new GetCtaCteProveedorReportQuery(proveedorId ?? Guid.Empty, from, to), ct), d => d.ToReportTable());
            case "ventas-bar":
                return ToReportTable(await _ventasPorBar.HandleAsync(new GetVentasPorBarReportQuery(barId, from, to), ct), d => d.ToReportTable());
            case "ventas-periodo":
                return ToReportTable(await _ventasPeriodo.HandleAsync(new GetVentasPeriodoReportQuery(from, to, agrupacion ?? "dia"), ct), d => d.ToReportTable());
            case "ventas-devoluciones":
                return ToReportTable(await _devoluciones.HandleAsync(new GetDevolucionesReportQuery(barId, from, to), ct), d => d.ToReportTable());
            case "ventas-cta-cte-bar":
                return ToReportTable(await _ctaCteBar.HandleAsync(new GetCtaCteBarReportQuery(barId ?? Guid.Empty, from, to), ct), d => d.ToReportTable());
            case "costos-producto":
                return ToReportTable(await _costoProducto.HandleAsync(new GetCostoProductoReportQuery(productoId, from, to), ct), d => d.ToReportTable());
            case "rentabilidad-producto":
                return ToReportTable(await _rentabilidadProducto.HandleAsync(new GetRentabilidadProductoReportQuery(productoId, from, to), ct), d => d.ToReportTable());
            case "rentabilidad-bar":
                return ToReportTable(await _rentabilidadBar.HandleAsync(new GetRentabilidadBarReportQuery(barId, from, to), ct), d => d.ToReportTable());
            case "planilla-costos":
                if (!recetaId.HasValue)
                {
                    return null;
                }
                return ToReportTable(await _planillaCostos.HandleAsync(new GetPlanillaCostosReportQuery(recetaId.Value), ct), d => d.ToReportTable());
            case "pedidos-detalle":
                return ToReportTable(await _pedidosDetalle.HandleAsync(new GetPedidosDetalleReportQuery(barId, from, to), ct), d => d.ToReportTable());
            case "pedidos-matriz":
                var matrizFrom = from ?? DateTime.Today.AddDays(-6);
                var matrizTo = to ?? DateTime.Today;
                return ToReportTable(await _matrizSemanal.HandleAsync(new GetMatrizSemanalReportQuery(barId, matrizFrom, matrizTo), ct), d => d.ToReportTable());
            default:
                return null;
        }
    }

    private static ReportTable? ToReportTable<TDto>(
        Result<TDto> result,
        Func<TDto, ReportTable> mapper)
        => result.IsSuccess ? mapper(result.Value) : null;
}
