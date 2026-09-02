using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.Devoluciones.Commands.CreateDevolucion;
using CentroDeProduccion.Application.Features.Devoluciones.Queries.GetDevolucionById;
using CentroDeProduccion.Application.Features.Devoluciones.Queries.GetDevolucionList;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/devoluciones")]
[Authorize(Roles = "Administrador,EncargadoVentas")]
public class DevolucionesController : ControllerBase
{
    private readonly CreateDevolucionCommandHandler _createHandler;
    private readonly GetDevolucionByIdQueryHandler _getByIdHandler;
    private readonly GetDevolucionListQueryHandler _getListHandler;

    public DevolucionesController(
        CreateDevolucionCommandHandler createHandler,
        GetDevolucionByIdQueryHandler getByIdHandler,
        GetDevolucionListQueryHandler getListHandler)
    {
        _createHandler = createHandler;
        _getByIdHandler = getByIdHandler;
        _getListHandler = getListHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDevolucionCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response));
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid? remitoId,
        [FromQuery] Guid? barId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        CancellationToken cancellationToken)
    {
        var result = await _getListHandler.HandleAsync(
            new GetDevolucionListQuery(remitoId, barId, fechaDesde, fechaHasta), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(new GetDevolucionByIdQuery(id), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }
}