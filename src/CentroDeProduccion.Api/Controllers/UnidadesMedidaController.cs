using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.UnidadesMedida.Commands.CreateUnidadMedida;
using CentroDeProduccion.Application.Features.UnidadesMedida.Commands.UpdateUnidadMedida;
using CentroDeProduccion.Application.Features.UnidadesMedida.Commands.DeactivateUnidadMedida;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UnidadesMedidaController : ControllerBase
{
    private readonly IUnidadMedidaRepository _unidadMedidaRepository;
    private readonly CreateUnidadMedidaCommandHandler _createHandler;
    private readonly UpdateUnidadMedidaCommandHandler _updateHandler;
    private readonly DeactivateUnidadMedidaCommandHandler _deactivateHandler;

    public UnidadesMedidaController(
        IUnidadMedidaRepository unidadMedidaRepository,
        CreateUnidadMedidaCommandHandler createHandler,
        UpdateUnidadMedidaCommandHandler updateHandler,
        DeactivateUnidadMedidaCommandHandler deactivateHandler)
    {
        _unidadMedidaRepository = unidadMedidaRepository;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deactivateHandler = deactivateHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var unidades = await _unidadMedidaRepository.GetAllActiveAsync(cancellationToken);
        return Ok(unidades);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var unidad = await _unidadMedidaRepository.GetByIdAsync(id, cancellationToken);
        if (unidad == null)
            return NotFound();

        return Ok(unidad);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUnidadMedidaCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUnidadMedidaCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _deactivateHandler.HandleAsync(new DeactivateUnidadMedidaCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
