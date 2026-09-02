using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.Empleados.Commands.CreateEmpleado;
using CentroDeProduccion.Application.Features.Empleados.Commands.UpdateEmpleado;
using CentroDeProduccion.Application.Features.Empleados.Commands.DeleteEmpleado;
using CentroDeProduccion.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmpleadosController : ControllerBase
{
    private readonly IEmpleadoRepository _empleadoRepository;
    private readonly CreateEmpleadoCommandHandler _createHandler;
    private readonly UpdateEmpleadoCommandHandler _updateHandler;
    private readonly DeleteEmpleadoCommandHandler _deleteHandler;

    public EmpleadosController(
        IEmpleadoRepository empleadoRepository,
        CreateEmpleadoCommandHandler createHandler,
        UpdateEmpleadoCommandHandler updateHandler,
        DeleteEmpleadoCommandHandler deleteHandler)
    {
        _empleadoRepository = empleadoRepository;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _deleteHandler = deleteHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool? activo = true,
        [FromQuery] CargoEmpleado? cargo = null,
        [FromQuery] CategoriaEmpleado? categoria = null,
        CancellationToken cancellationToken = default)
    {
        var empleados = await _empleadoRepository.GetAllAsync(activo, cargo, categoria, cancellationToken);
        return Ok(empleados);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var empleado = await _empleadoRepository.GetByIdAsync(id, cancellationToken);
        if (empleado == null)
            return NotFound();

        return Ok(empleado);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateEmpleadoCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response));
    }

    [Authorize(Roles = "Administrador")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmpleadoCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [Authorize(Roles = "Administrador")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromBody] DeleteEmpleadoCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _deleteHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this);
    }
}