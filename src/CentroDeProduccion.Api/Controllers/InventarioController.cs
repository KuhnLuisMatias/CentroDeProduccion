using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.Inventario.Commands.ConfirmInventarioSesion;
using CentroDeProduccion.Application.Features.Inventario.Commands.CreateInventarioSesion;
using CentroDeProduccion.Application.Features.Inventario.Commands.RegistrarConteo;
using CentroDeProduccion.Application.Features.Inventario.Queries.GetInventarioSesionById;
using CentroDeProduccion.Application.Features.Inventario.Queries.GetInventarioSesionList;
using CentroDeProduccion.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/stock/inventario")]
[Authorize(Roles = "Administrador,EncargadoProduccion")]
public class InventarioController : ControllerBase
{
    private readonly CreateInventarioSesionCommandHandler _createHandler;
    private readonly RegistrarConteoCommandHandler _registrarConteoHandler;
    private readonly ConfirmInventarioSesionCommandHandler _confirmHandler;
    private readonly GetInventarioSesionByIdQueryHandler _getByIdHandler;
    private readonly GetInventarioSesionListQueryHandler _getListHandler;

    public InventarioController(
        CreateInventarioSesionCommandHandler createHandler,
        RegistrarConteoCommandHandler registrarConteoHandler,
        ConfirmInventarioSesionCommandHandler confirmHandler,
        GetInventarioSesionByIdQueryHandler getByIdHandler,
        GetInventarioSesionListQueryHandler getListHandler)
    {
        _createHandler = createHandler;
        _registrarConteoHandler = registrarConteoHandler;
        _confirmHandler = confirmHandler;
        _getByIdHandler = getByIdHandler;
        _getListHandler = getListHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateInventarioSesionCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpPost("{id:guid}/conteos")]
    public async Task<IActionResult> RegistrarConteo(
        Guid id, [FromBody] RegistrarConteoCommand command, CancellationToken cancellationToken)
    {
        if (id != command.InventarioSesionId)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _registrarConteoHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpPost("{id:guid}/confirmar")]
    public async Task<IActionResult> Confirmar(
        Guid id, [FromBody] ConfirmInventarioSesionCommand command, CancellationToken cancellationToken)
    {
        if (id != command.InventarioSesionId)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _confirmHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(new GetInventarioSesionByIdQuery(id), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] EstadoInventario? estado,
        [FromQuery] TipoInventario? tipo,
        [FromQuery] DateTime? desde,
        [FromQuery] DateTime? hasta,
        CancellationToken cancellationToken)
    {
        var result = await _getListHandler.HandleAsync(
            new GetInventarioSesionListQuery(estado, tipo, desde, hasta), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }
}
