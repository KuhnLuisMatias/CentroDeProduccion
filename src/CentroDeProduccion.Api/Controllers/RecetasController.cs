using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.Recetas.Commands.CreateReceta;
using CentroDeProduccion.Application.Features.Recetas.Commands.UpdateReceta;
using CentroDeProduccion.Application.Features.Recetas.Queries.CalcularCosto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RecetasController : ControllerBase
{
    private readonly IRecetaRepository _recetaRepository;
    private readonly CreateRecetaCommandHandler _createHandler;
    private readonly UpdateRecetaCommandHandler _updateHandler;
    private readonly CalcularCostoRecetaHandler _calcularCostoHandler;

    public RecetasController(
        IRecetaRepository recetaRepository,
        CreateRecetaCommandHandler createHandler,
        UpdateRecetaCommandHandler updateHandler,
        CalcularCostoRecetaHandler calcularCostoHandler)
    {
        _recetaRepository = recetaRepository;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _calcularCostoHandler = calcularCostoHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var recetas = await _recetaRepository.GetAllActiveAsync(cancellationToken);
        return Ok(recetas);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var receta = await _recetaRepository.GetByIdWithDetallesAsync(id, cancellationToken);
        if (receta == null)
            return NotFound();

        return Ok(receta);
    }

    [HttpGet("{id:guid}/costeo")]
    public async Task<IActionResult> CalcularCosto(Guid id, CancellationToken cancellationToken)
    {
        var result = await _calcularCostoHandler.HandleAsync(new CalcularCostoRecetaQuery(id), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<IActionResult> GetVersiones(Guid id, CancellationToken cancellationToken)
    {
        var versiones = await _recetaRepository.GetVersionesAsync(id, cancellationToken);
        return Ok(versiones);
    }

    [Authorize(Roles = "Administrador,EncargadoProduccion")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecetaCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response));
    }

    [Authorize(Roles = "Administrador,EncargadoProduccion")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRecetaCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this);
    }
}
