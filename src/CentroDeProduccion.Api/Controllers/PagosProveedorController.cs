using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.Pagos.Commands.CreatePagoProveedor;
using CentroDeProduccion.Application.Features.Pagos.Queries.GetPagoById;
using CentroDeProduccion.Application.Features.Pagos.Queries.GetPagoList;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

/// <summary>
/// Facturas de compra a proveedores: the real purchase document that sums insumo stock and
/// generates supplier debt. Route kept as /api/pagos-proveedor for backward compatibility.
/// </summary>
[ApiController]
[Route("api/pagos-proveedor")]
[Authorize]
public class PagosProveedorController : ControllerBase
{
    private readonly CreatePagoProveedorCommandHandler _createHandler;
    private readonly GetPagoByIdQueryHandler _getByIdHandler;
    private readonly GetPagoListQueryHandler _getListHandler;

    public PagosProveedorController(
        CreatePagoProveedorCommandHandler createHandler,
        GetPagoByIdQueryHandler getByIdHandler,
        GetPagoListQueryHandler getListHandler)
    {
        _createHandler = createHandler;
        _getByIdHandler = getByIdHandler;
        _getListHandler = getListHandler;
    }

    /// <summary>Creates a factura de compra: records insumo lines, payment methods, Compra
    /// stock movements and one Compra (debt) movement in the supplier's cuenta corriente.</summary>
    [HttpPost]
    [Authorize(Roles = "Administrador,EncargadoCompras")]
    public async Task<IActionResult> Create([FromBody] CreatePagoProveedorCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    /// <summary>Lists facturas de compra with optional supplier/date filters.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid? proveedorId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        CancellationToken cancellationToken)
    {
        var result = await _getListHandler.HandleAsync(
            new GetPagoListQuery(proveedorId, fechaDesde, fechaHasta), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    /// <summary>Gets one factura de compra with its insumo lines and payment methods.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(new GetPagoByIdQuery(id), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }
}
