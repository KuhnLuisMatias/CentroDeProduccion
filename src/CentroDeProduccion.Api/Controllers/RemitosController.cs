using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.Remitos.Commands.CancelarRemito;
using CentroDeProduccion.Application.Features.Remitos.Commands.ConfirmRemito;
using CentroDeProduccion.Application.Features.Remitos.Commands.CreateRemito;
using CentroDeProduccion.Application.Features.Remitos.Commands.UpdateEstadoRemito;
using CentroDeProduccion.Application.Features.Remitos.Commands.UpdateRemito;
using CentroDeProduccion.Application.Features.Remitos.Queries.GetRemitoById;
using CentroDeProduccion.Application.Features.Remitos.Queries.GetRemitoList;
using CentroDeProduccion.Application.Features.Remitos.Queries.GetOrdenCarga;
using CentroDeProduccion.Application.Features.Remitos.Queries.GetRemitoPrint;
using CentroDeProduccion.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/remitos")]
[Authorize(Roles = "Administrador,EncargadoVentas")]
public class RemitosController : ControllerBase
{
    private readonly CreateRemitoCommandHandler _createHandler;
    private readonly UpdateRemitoCommandHandler _updateHandler;
    private readonly UpdateEstadoRemitoCommandHandler _updateEstadoHandler;
    private readonly CancelarRemitoCommandHandler _cancelarHandler;
    private readonly ConfirmRemitoCommandHandler _confirmarHandler;
    private readonly GetRemitoByIdQueryHandler _getByIdHandler;
    private readonly GetRemitoListQueryHandler _getListHandler;
    private readonly GetRemitoPrintQueryHandler _getPrintHandler;
    private readonly GetOrdenCargaQueryHandler _getOrdenCargaHandler;

    public RemitosController(
        CreateRemitoCommandHandler createHandler,
        UpdateRemitoCommandHandler updateHandler,
        UpdateEstadoRemitoCommandHandler updateEstadoHandler,
        CancelarRemitoCommandHandler cancelarHandler,
        ConfirmRemitoCommandHandler confirmarHandler,
        GetRemitoByIdQueryHandler getByIdHandler,
        GetRemitoListQueryHandler getListHandler,
        GetRemitoPrintQueryHandler getPrintHandler,
        GetOrdenCargaQueryHandler getOrdenCargaHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _updateEstadoHandler = updateEstadoHandler;
        _cancelarHandler = cancelarHandler;
        _confirmarHandler = confirmarHandler;
        _getByIdHandler = getByIdHandler;
        _getListHandler = getListHandler;
        _getPrintHandler = getPrintHandler;
        _getOrdenCargaHandler = getOrdenCargaHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRemitoCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response));
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid? barId,
        [FromQuery] EstadoRemito? estado,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        CancellationToken cancellationToken)
    {
        var result = await _getListHandler.HandleAsync(
            new GetRemitoListQuery(barId, estado, fechaDesde, fechaHasta), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(new GetRemitoByIdQuery(id), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRemitoCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPut("{id:guid}/estado")]
    public async Task<IActionResult> UpdateEstado(Guid id, [FromBody] UpdateEstadoRemitoCommand command, CancellationToken cancellationToken)
    {
        if (id != command.RemitoId)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _updateEstadoHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:guid}/cancelar")]
    public async Task<IActionResult> Cancelar(Guid id, [FromBody] CancelarRemitoCommand command, CancellationToken cancellationToken)
    {
        if (id != command.RemitoId)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _cancelarHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:guid}/confirmar")]
    public async Task<IActionResult> Confirmar(Guid id, [FromBody] ConfirmRemitoCommand command, CancellationToken cancellationToken)
    {
        if (id != command.RemitoId)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _confirmarHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpGet("{id:guid}/imprimir")]
    public async Task<IActionResult> Imprimir(Guid id, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        var result = await _getPrintHandler.HandleAsync(new GetRemitoPrintQuery(id, format ?? "a4"), cancellationToken);
        return result.ToActionResult(this, response => Content(response.Html, "text/html"));
    }

    [HttpGet("{id:guid}/orden-carga")]
    public async Task<IActionResult> OrdenCarga(Guid id, [FromQuery] string? format, CancellationToken cancellationToken)
    {
        var result = await _getOrdenCargaHandler.HandleAsync(new GetOrdenCargaQuery(id, format ?? "a4"), cancellationToken);
        return result.ToActionResult(this, response => Content(response.Html, "text/html"));
    }
}