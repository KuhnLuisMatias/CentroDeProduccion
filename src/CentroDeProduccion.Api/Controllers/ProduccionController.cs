using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Application.Features.Produccion.Commands.CreateProduccion;
using CentroDeProduccion.Application.Features.Produccion.Commands.ConfirmProduccion;
using CentroDeProduccion.Application.Features.Produccion.Commands.CancelProduccion;
using CentroDeProduccion.Application.Features.Produccion.Commands.EditarInsumosProduccion;
using CentroDeProduccion.Application.Features.Produccion.Queries.GetProduccionById;
using CentroDeProduccion.Application.Features.Produccion.Queries.GetProducciones;
using CentroDeProduccion.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,EncargadoProduccion")]
public class ProduccionController : ControllerBase
{
    private readonly IProduccionRepository _produccionRepository;
    private readonly ProductoTerminadoCostoResolver _costoResolver;
    private readonly CreateProduccionCommandHandler _createHandler;
    private readonly ConfirmProduccionCommandHandler _confirmHandler;
    private readonly CancelProduccionCommandHandler _cancelHandler;
    private readonly EditarInsumosProduccionCommandHandler _editarInsumosHandler;

    public ProduccionController(
        IProduccionRepository produccionRepository,
        ProductoTerminadoCostoResolver costoResolver,
        CreateProduccionCommandHandler createHandler,
        ConfirmProduccionCommandHandler confirmHandler,
        CancelProduccionCommandHandler cancelHandler,
        EditarInsumosProduccionCommandHandler editarInsumosHandler)
    {
        _produccionRepository = produccionRepository;
        _costoResolver = costoResolver;
        _createHandler = createHandler;
        _confirmHandler = confirmHandler;
        _cancelHandler = cancelHandler;
        _editarInsumosHandler = editarInsumosHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var producciones = await _produccionRepository.GetAllAsync(cancellationToken);

        // Batch the live sub-PT costs once for every sub-recipe line of Borrador runs.
        var borradores = producciones.Where(p => p.Estado == EstadoProduccion.Borrador).ToList();
        var costosSubPt = await _costoResolver.CalcularPorRecetasAsync(
            borradores
                .SelectMany(p => p.InsumosConsumidos)
                .Where(pi => pi.RecetaOrigenId.HasValue)
                .Select(pi => (Guid?)pi.RecetaOrigenId!.Value)
                .Distinct(),
            cancellationToken);

        // Explicit mapping: never serialize the entity graph (Usuario.PasswordHash leak via
        // Responsable navigation). Same field names as GetProduccionByIdResponse minus salidas.
        var response = producciones.Select(p =>
        {
            // Borrador runs don't persist costs (written only on confirm); compute the estimate
            // on read so line edits are reflected (same formula as ConfirmProduccionCommandHandler:
            // insumos at last purchase price + sub-recipe lines at the sub-PT's live cost).
            var esBorrador = p.Estado == EstadoProduccion.Borrador;
            var costoInsumos = esBorrador
                ? p.InsumosConsumidos?.Sum(pi => (pi.Insumo?.PrecioUltimaCompra ?? 0m) * pi.Cantidad
                    + (pi.RecetaOrigenId.HasValue
                        ? costosSubPt.GetValueOrDefault(pi.RecetaOrigenId.Value) * pi.Cantidad
                        : 0m)) ?? 0m
                : p.CostoTotalInsumos;

            // Borrador shows quantity 1, so unit cost = the estimated batch total.
            var costoUnitario = esBorrador
                ? costoInsumos
                : p.CantidadProducida > 0 ? p.CostoTotal / p.CantidadProducida : 0m;

            return new GetProduccionListItemResponse(
                p.Id,
                p.RecetaId,
                p.Receta is null ? null : new ProduccionRecetaInfo(p.Receta.Id, p.Receta.Nombre, p.Receta.UnidadMedida?.Simbolo),
                p.Lote,
                p.Fecha,
                p.ResponsableId,
                new ProduccionResponsableInfo(p.ResponsableId, p.Responsable?.Nombre ?? string.Empty, p.Responsable?.Apellido ?? string.Empty),
                p.Estado,
                p.Observaciones,
                // Display-only: Borrador runs show 1 unit so rows aren't empty; the persisted
                // column stays 0 until confirm so dashboard sums aren't polluted.
                esBorrador ? 1m : p.CantidadProducida,
                p.FechaVencimiento,
                costoInsumos,
                esBorrador ? costoInsumos : p.CostoTotal,
                p.RowVersion,
                costoUnitario);
        }).ToList();

        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var produccion = await _produccionRepository.GetByIdWithSalidasAsync(id, cancellationToken);
        if (produccion == null)
            return NotFound();

        // Explicit mapping: never serialize the entity graph (Usuario.PasswordHash leak +
        // Produccion↔Salidas recursion). Shape matches frontend/src/lib/types.ts Produccion.
        // Display-only (same as GetAll): Borrador shows 1 unit without persisting it.
        var esBorrador = produccion.Estado == EstadoProduccion.Borrador;
        // Live unit cost per line: insumo lines at last purchase price, sub-recipe lines at the
        // sub-PT's live standard cost (same source remitos price with) so the frontend shows it.
        var costosSubPt = await _costoResolver.CalcularPorRecetasAsync(
            produccion.InsumosConsumidos
                .Where(i => i.RecetaOrigenId.HasValue)
                .Select(i => (Guid?)i.RecetaOrigenId!.Value)
                .Distinct(),
            cancellationToken);
        var response = new GetProduccionByIdResponse(
            produccion.Id,
            produccion.RecetaId,
            produccion.Receta is null
                ? null!
                : new ProduccionRecetaInfo(produccion.Receta.Id, produccion.Receta.Nombre, produccion.Receta.UnidadMedida?.Simbolo),
            produccion.Lote,
            produccion.Fecha,
            produccion.ResponsableId,
            new ProduccionResponsableInfo(produccion.ResponsableId, produccion.Responsable?.Nombre ?? string.Empty, produccion.Responsable?.Apellido ?? string.Empty),
            produccion.Estado,
            produccion.Observaciones,
            esBorrador ? 1m : produccion.CantidadProducida,
            produccion.FechaVencimiento,
            produccion.CostoTotalInsumos,
            produccion.CostoTotal,
            produccion.RowVersion,
            produccion.Salidas.Select(s => new ProduccionSalidaResponse(
                s.Id,
                s.ProduccionId,
                s.ProductoTerminadoId,
                s.ProductoTerminado is null
                    ? null
                    : new ProduccionSalidaProductoInfo(s.ProductoTerminado.Id, s.ProductoTerminado.Nombre, s.ProductoTerminado.CodigoSku),
                s.Cantidad,
                s.TipoSalida)).ToList(),
            produccion.InsumosConsumidos.Select(i => new ProduccionInsumoResponse(
                i.Id,
                i.ProduccionId,
                i.InsumoId,
                i.Insumo is null
                    ? null
                    : new ProduccionInsumoInsumoInfo(i.Insumo.Id, i.Insumo.Nombre, i.Insumo.CodigoSku, i.Insumo.UnidadConsumoId),
                i.RecetaOrigenId,
                i.RecetaOrigen is null
                    ? null
                    : new ProduccionInsumoRecetaInfo(i.RecetaOrigen.Id, i.RecetaOrigen.Nombre, i.RecetaOrigen.UnidadMedida?.Simbolo),
                i.Cantidad,
                i.InsumoId.HasValue
                    ? i.Insumo?.PrecioUltimaCompra ?? 0m
                    : costosSubPt.GetValueOrDefault(i.RecetaOrigenId!.Value),
                i.Observaciones)).ToList());

        return Ok(response);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProduccionCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response));
    }

    /// <summary>Replaces the full insumo-consumption list of a Borrador production run.</summary>
    [HttpPut("{id:guid}/insumos")]
    public async Task<IActionResult> EditarInsumos(Guid id, [FromBody] EditarInsumosProduccionCommand command, CancellationToken cancellationToken)
    {
        if (id != command.ProduccionId)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _editarInsumosHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, [FromBody] ConfirmProduccionCommand command, CancellationToken cancellationToken)
    {
        if (id != command.ProduccionId)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _confirmHandler.HandleAsync(command with { ProduccionId = id }, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelProduccionCommand command, CancellationToken cancellationToken)
    {
        if (id != command.ProduccionId)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _cancelHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }
}
