using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.Bares.Commands.CreateBar;
using CentroDeProduccion.Application.Features.Bares.Commands.DeleteBar;
using CentroDeProduccion.Application.Features.Bares.Commands.ReactivateBar;
using CentroDeProduccion.Application.Features.Bares.Commands.UpdateBar;
using CentroDeProduccion.Application.Features.Bares.Queries.GetBarById;
using CentroDeProduccion.Application.Features.Bares.Queries.GetBarList;
using CentroDeProduccion.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/bares")]
[Authorize(Roles = "Administrador,EncargadoVentas")]
public class BaresController : ControllerBase
{
    private readonly CreateBarCommandHandler _createHandler;
    private readonly UpdateBarCommandHandler _updateHandler;
    private readonly DeleteBarCommandHandler _deleteHandler;
    private readonly ReactivateBarCommandHandler _reactivateHandler;
    private readonly GetBarByIdQueryHandler _getByIdHandler;
    private readonly GetBarListQueryHandler _getListHandler;

    public BaresController(
        CreateBarCommandHandler createHandler,
        UpdateBarCommandHandler updateHandler,
        DeleteBarCommandHandler deleteHandler,
        ReactivateBarCommandHandler reactivateHandler,
        GetBarByIdQueryHandler getByIdHandler,
        GetBarListQueryHandler getListHandler)
    {
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
        _reactivateHandler = reactivateHandler;
        _getByIdHandler = getByIdHandler;
        _getListHandler = getListHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBarCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response));
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] EstadoBar? estado,
        [FromQuery] string? searchTerm,
        CancellationToken cancellationToken)
    {
        var result = await _getListHandler.HandleAsync(new GetBarListQuery(estado, searchTerm), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(new GetBarByIdQuery(id), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBarCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromBody] DeleteBarCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _deleteHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpPost("{id:guid}/reactivar")]
    public async Task<IActionResult> Reactivar(Guid id, [FromBody] ReactivateBarCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _reactivateHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this);
    }
}