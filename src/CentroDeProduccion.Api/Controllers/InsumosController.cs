using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.Insumos.Commands.CreateInsumo;
using CentroDeProduccion.Application.Features.Insumos.Commands.ReactivateInsumo;
using CentroDeProduccion.Application.Features.Insumos.Commands.UpdateInsumo;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador,EncargadoProduccion,EncargadoCompras")]
public class InsumosController : ControllerBase
{
    private readonly IInsumoRepository _insumoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateInsumoCommandHandler _createHandler;
    private readonly UpdateInsumoCommandHandler _updateHandler;
    private readonly ReactivateInsumoCommandHandler _reactivateHandler;

    public InsumosController(
        IInsumoRepository insumoRepository,
        IUnitOfWork unitOfWork,
        CreateInsumoCommandHandler createHandler,
        UpdateInsumoCommandHandler updateHandler,
        ReactivateInsumoCommandHandler reactivateHandler)
    {
        _insumoRepository = insumoRepository;
        _unitOfWork = unitOfWork;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _reactivateHandler = reactivateHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var result = await _insumoRepository.GetPagedAsync(search, page, pageSize, includeInactive, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var insumo = await _insumoRepository.GetByIdAsync(id, cancellationToken);
        if (insumo == null)
            return NotFound();

        return Ok(insumo);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInsumoCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInsumoCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var insumo = await _insumoRepository.GetByIdAsync(id, cancellationToken);
        if (insumo == null)
            return NotFound();

        insumo.Activo = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/reactivar")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Reactivar(Guid id, CancellationToken cancellationToken)
    {
        var result = await _reactivateHandler.HandleAsync(new ReactivateInsumoCommand(id), cancellationToken);
        return result.ToActionResult(this);
    }
}
