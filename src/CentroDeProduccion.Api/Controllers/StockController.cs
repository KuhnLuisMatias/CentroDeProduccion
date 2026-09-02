using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.OrdenesCompra.Commands.GenerarOCDesdeAlertas;
using CentroDeProduccion.Application.Features.Stock.Commands.RegisterMovement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,EncargadoProduccion,EncargadoCompras")]
public class StockController : ControllerBase
{
    private readonly IInsumoRepository _insumoRepository;
    private readonly IMovimientoStockRepository _movimientoStockRepository;
    private readonly RegisterMovementCommandHandler _registerMovementHandler;
    private readonly GenerarOCDesdeAlertasCommandHandler _generarOCDesdeAlertasHandler;

    public StockController(
        IInsumoRepository insumoRepository,
        IMovimientoStockRepository movimientoStockRepository,
        RegisterMovementCommandHandler registerMovementHandler,
        GenerarOCDesdeAlertasCommandHandler generarOCDesdeAlertasHandler)
    {
        _insumoRepository = insumoRepository;
        _movimientoStockRepository = movimientoStockRepository;
        _registerMovementHandler = registerMovementHandler;
        _generarOCDesdeAlertasHandler = generarOCDesdeAlertasHandler;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var (total, critical) = await _insumoRepository.GetActiveCountAsync(cancellationToken);
        return Ok(new
        {
            TotalInsumosActivos = total,
            InsumosCriticos = critical
        });
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts(CancellationToken cancellationToken)
    {
        var insumos = await _insumoRepository.GetAllActiveAsync(cancellationToken);
        var alerts = insumos
            .Where(i => i.StockActual <= i.StockMinimo)
            .Select(i => new
            {
                i.Id,
                i.Nombre,
                i.CodigoSku,
                i.StockActual,
                i.StockMinimo,
                i.UnidadConsumo?.Simbolo,
                i.ProveedorPrincipal?.NombreRazonSocial,
                i.ProveedorPrincipal?.Telefono,
                i.ProveedorPrincipal?.WhatsApp,
                i.PrecioUltimaCompra
            });
        return Ok(alerts);
    }

    [HttpGet("insumo/{insumoId:guid}/movements")]
    public async Task<IActionResult> GetMovements(Guid insumoId, CancellationToken cancellationToken)
    {
        var movements = await _movimientoStockRepository.GetByInsumoIdAsync(insumoId, cancellationToken);
        return Ok(movements);
    }

    [HttpPost("movement")]
    public async Task<IActionResult> RegisterMovement([FromBody] RegisterMovementCommand command, CancellationToken cancellationToken)
    {
        var result = await _registerMovementHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpPost("alertas/generar-oc")]
    [Authorize(Roles = "Administrador,EncargadoCompras")]
    public async Task<IActionResult> GenerarOCDesdeAlertas(
        [FromBody] GenerarOCDesdeAlertasCommand command, CancellationToken cancellationToken)
    {
        var result = await _generarOCDesdeAlertasHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response.Ordenes));
    }
}
