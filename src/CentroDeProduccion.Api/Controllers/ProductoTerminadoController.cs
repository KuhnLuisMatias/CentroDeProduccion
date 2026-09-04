using CentroDeProduccion.Domain.Services;
using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Application.Common;
using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.ProductosTerminados.Commands.CreateProductoTerminado;
using CentroDeProduccion.Application.Features.ProductosTerminados.Commands.UpdateProductoTerminado;
using CentroDeProduccion.Application.Features.ProductosTerminados.Commands.ReserveStock;
using CentroDeProduccion.Application.Features.ProductosTerminados.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductoTerminadoController : ControllerBase
{
    private readonly IProductoTerminadoRepository _productoTerminadoRepository;
    private readonly ProductoTerminadoCostoResolver _costoResolver;
    private readonly CreateProductoTerminadoCommandHandler _createHandler;
    private readonly UpdateProductoTerminadoCommandHandler _updateHandler;
    private readonly ReserveStockCommandHandler _reserveHandler;

    public ProductoTerminadoController(
        IProductoTerminadoRepository productoTerminadoRepository,
        ProductoTerminadoCostoResolver costoResolver,
        CreateProductoTerminadoCommandHandler createHandler,
        UpdateProductoTerminadoCommandHandler updateHandler,
        ReserveStockCommandHandler reserveHandler)
    {
        _productoTerminadoRepository = productoTerminadoRepository;
        _costoResolver = costoResolver;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _reserveHandler = reserveHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var productos = await _productoTerminadoRepository.GetAllActiveAsync(cancellationToken);

        // CostoUnitario computed live from the recipe BOM at current insumo prices
        // via ProductoTerminadoCostoResolver (0 when the product has no recipe).
        var costos = await _costoResolver.CalcularPorRecetasAsync(
            productos.Select(p => p.RecetaId), cancellationToken);

        return Ok(productos.Select(p => Map(
            p,
            p.RecetaId is { } recetaId ? costos.GetValueOrDefault(recetaId) : 0m)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var producto = await _productoTerminadoRepository.GetByIdAsync(id, cancellationToken);
        if (producto == null)
            return NotFound();

        var costo = await _costoResolver.CalcularPorRecetaAsync(producto.RecetaId, cancellationToken);
        return Ok(Map(producto, costo));
    }

    [HttpGet("expiring")]
    public async Task<IActionResult> GetProximosAVencer([FromQuery] int dias = 7, CancellationToken cancellationToken = default)
    {
        var hasta = RelojDeNegocio.Ahora.AddDays(dias);
        var productos = await _productoTerminadoRepository.GetProximosAVencerAsync(hasta, cancellationToken);
        return Ok(productos);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductoTerminadoCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductoTerminadoCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:guid}/reserve")]
    public async Task<IActionResult> Reserve(Guid id, [FromBody] ReserveStockCommand command, CancellationToken cancellationToken)
    {
        if (id != command.ProductoTerminadoId)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _reserveHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    private static ProductoTerminadoResponse Map(Domain.Entities.ProductoTerminado p, decimal costoUnitario) => new(
        p.Id,
        p.Nombre,
        p.CodigoSku,
        p.CategoriaId,
        p.UnidadMedidaId,
        p.StockActual,
        p.StockMinimo,
        costoUnitario,
        p.FechaProduccion,
        p.FechaVencimiento,
        p.Lote,
        p.Estado,
        p.Activo,
        p.FechaCreacion,
        p.Categoria is null ? null : new ProductoTerminadoCategoriaInfo(p.Categoria.Id, p.Categoria.Nombre),
        p.UnidadMedida is null
            ? null
            : new ProductoTerminadoUnidadInfo(p.UnidadMedida.Id, p.UnidadMedida.Nombre, p.UnidadMedida.Simbolo),
        p.RecetaId,
        p.Receta is null ? null : new ProductoTerminadoRecetaInfo(p.Receta.Id, p.Receta.Nombre));
}

