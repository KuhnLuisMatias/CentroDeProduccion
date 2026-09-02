using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.OrdenesCompra.Commands.CancelarOrdenCompra;
using CentroDeProduccion.Application.Features.OrdenesCompra.Commands.CreateOrdenCompra;
using CentroDeProduccion.Application.Features.OrdenesCompra.Commands.EnviarOrdenCompra;
using CentroDeProduccion.Application.Features.OrdenesCompra.Commands.UpdateOrdenCompra;
using CentroDeProduccion.Application.Features.OrdenesCompra.Queries.GetOrdenCompraById;
using CentroDeProduccion.Application.Features.OrdenesCompra.Queries.GetOrdenCompraList;
using CentroDeProduccion.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/ordenes-compra")]
[Authorize(Roles = "Administrador,EncargadoCompras")]
public class OrdenesCompraController : ControllerBase
{
    private readonly CreateOrdenCompraCommandHandler _createHandler;
    private readonly UpdateOrdenCompraCommandHandler _updateHandler;
    private readonly EnviarOrdenCompraCommandHandler _enviarHandler;
    private readonly CancelarOrdenCompraCommandHandler _cancelarHandler;
    private readonly GetOrdenCompraByIdQueryHandler _getByIdHandler;
    private readonly GetOrdenCompraListQueryHandler _getListHandler;

    public OrdenesCompraController(
        CreateOrdenCompraCommandHandler createHandler,
        UpdateOrdenCompraCommandHandler updateHandler,
        EnviarOrdenCompraCommandHandler enviarHandler,
        CancelarOrdenCompraCommandHandler cancelarHandler,
        GetOrdenCompraByIdQueryHandler getByIdHandler,
        GetOrdenCompraListQueryHandler getListHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _enviarHandler = enviarHandler;
        _cancelarHandler = cancelarHandler;
        _getByIdHandler = getByIdHandler;
        _getListHandler = getListHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrdenCompraCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response));
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid? proveedorId,
        [FromQuery] EstadoOrdenCompra? estado,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        CancellationToken cancellationToken)
    {
        var result = await _getListHandler.HandleAsync(
            new GetOrdenCompraListQuery(proveedorId, estado, fechaDesde, fechaHasta), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(new GetOrdenCompraByIdQuery(id), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateOrdenCompraCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:guid}/enviar")]
    public async Task<IActionResult> Enviar(Guid id, [FromBody] EnviarOrdenCompraCommand command, CancellationToken cancellationToken)
    {
        if (id != command.OrdenCompraId)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _enviarHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, [FromBody] CancelarOrdenCompraCommand command, CancellationToken cancellationToken)
    {
        if (id != command.OrdenCompraId)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _cancelarHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }
}