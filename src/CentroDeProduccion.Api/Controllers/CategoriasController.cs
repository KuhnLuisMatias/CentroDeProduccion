using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.Categorias.Commands.CreateCategoria;
using CentroDeProduccion.Application.Features.Categorias.Commands.UpdateCategoria;
using CentroDeProduccion.Application.Features.Categorias.Commands.DeactivateCategoria;
using CentroDeProduccion.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoriasController : ControllerBase
{
    private readonly ICategoriaRepository _categoriaRepository;
    private readonly CreateCategoriaCommandHandler _createHandler;
    private readonly UpdateCategoriaCommandHandler _updateHandler;
    private readonly DeactivateCategoriaCommandHandler _deactivateHandler;

    public CategoriasController(
        ICategoriaRepository categoriaRepository,
        CreateCategoriaCommandHandler createHandler,
        UpdateCategoriaCommandHandler updateHandler,
        DeactivateCategoriaCommandHandler deactivateHandler)
    {
        _categoriaRepository = categoriaRepository;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deactivateHandler = deactivateHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] AmbitoCategoria? ambito, CancellationToken cancellationToken)
    {
        if (ambito.HasValue)
        {
            var categorias = await _categoriaRepository.GetAllByAmbitoAsync(ambito.Value, cancellationToken);
            return Ok(categorias);
        }

        var insumos = await _categoriaRepository.GetAllByAmbitoAsync(AmbitoCategoria.Insumo, cancellationToken);
        var productos = await _categoriaRepository.GetAllByAmbitoAsync(AmbitoCategoria.ProductoTerminado, cancellationToken);
        return Ok(new { Insumos = insumos, ProductosTerminados = productos });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var categoria = await _categoriaRepository.GetByIdAsync(id, cancellationToken);
        if (categoria == null)
            return NotFound();

        return Ok(categoria);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoriaCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoriaCommand command, CancellationToken cancellationToken)
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
        var result = await _deactivateHandler.HandleAsync(new DeactivateCategoriaCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
