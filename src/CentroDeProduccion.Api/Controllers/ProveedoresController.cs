using CentroDeProduccion.Application.Abstractions.Persistence;
using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.CuentaCorriente.Commands.RegisterNotaCredito;
using CentroDeProduccion.Application.Features.CuentaCorriente.Commands.RegisterNotaDebito;
using CentroDeProduccion.Application.Features.CuentaCorriente.Queries.GetEstadoCuenta;
using CentroDeProduccion.Application.Features.CuentaCorriente.Queries.GetMovimientos;
using CentroDeProduccion.Application.Features.CuentaCorriente.Queries.GetSaldo;
using CentroDeProduccion.Application.Features.Proveedores.Commands.CreateProveedor;
using CentroDeProduccion.Application.Features.Proveedores.Commands.UpdateProveedor;
using CentroDeProduccion.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProveedoresController : ControllerBase
{
    private readonly IProveedorRepository _proveedorRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateProveedorCommandHandler _createHandler;
    private readonly UpdateProveedorCommandHandler _updateHandler;
    private readonly GetEstadoCuentaQueryHandler _estadoCuentaHandler;
    private readonly GetMovimientosQueryHandler _movimientosHandler;
    private readonly GetSaldoQueryHandler _saldoHandler;
    private readonly RegisterNotaDebitoCommandHandler _notaDebitoHandler;
    private readonly RegisterNotaCreditoCommandHandler _notaCreditoHandler;

    public ProveedoresController(
        IProveedorRepository proveedorRepository,
        IUnitOfWork unitOfWork,
        CreateProveedorCommandHandler createHandler,
        UpdateProveedorCommandHandler updateHandler,
        GetEstadoCuentaQueryHandler estadoCuentaHandler,
        GetMovimientosQueryHandler movimientosHandler,
        GetSaldoQueryHandler saldoHandler,
        RegisterNotaDebitoCommandHandler notaDebitoHandler,
        RegisterNotaCreditoCommandHandler notaCreditoHandler)
    {
        _proveedorRepository = proveedorRepository;
        _unitOfWork = unitOfWork;
        _createHandler = createHandler;
        _updateHandler = updateHandler;
        _estadoCuentaHandler = estadoCuentaHandler;
        _movimientosHandler = movimientosHandler;
        _saldoHandler = saldoHandler;
        _notaDebitoHandler = notaDebitoHandler;
        _notaCreditoHandler = notaCreditoHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var proveedores = await _proveedorRepository.GetAllActiveAsync(cancellationToken);
        return Ok(proveedores);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var proveedor = await _proveedorRepository.GetByIdAsync(id, cancellationToken);
        if (proveedor == null)
            return NotFound();

        return Ok(proveedor);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador,EncargadoCompras")]
    public async Task<IActionResult> Create([FromBody] CreateProveedorCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => CreatedAtAction(nameof(GetById), new { id = response.Id }, response));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Administrador,EncargadoCompras")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProveedorCommand command, CancellationToken cancellationToken)
    {
        if (id != command.Id)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _updateHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Administrador,EncargadoCompras")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var proveedor = await _proveedorRepository.GetByIdAsync(id, cancellationToken);
        if (proveedor == null)
            return NotFound();

        proveedor.Activo = false;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("{id:guid}/cuenta-corriente")]
    public async Task<IActionResult> GetEstadoCuenta(
        Guid id,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        CancellationToken cancellationToken)
    {
        var result = await _estadoCuentaHandler.HandleAsync(
            new GetEstadoCuentaQuery(id, fechaDesde, fechaHasta), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpGet("{id:guid}/cuenta-corriente/movimientos")]
    public async Task<IActionResult> GetMovimientos(
        Guid id,
        [FromQuery] TipoMovimientoCtaCte? tipo,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        CancellationToken cancellationToken)
    {
        var result = await _movimientosHandler.HandleAsync(
            new GetMovimientosQuery(id, tipo, fechaDesde, fechaHasta), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpGet("{id:guid}/cuenta-corriente/saldo")]
    public async Task<IActionResult> GetSaldo(Guid id, CancellationToken cancellationToken)
    {
        var result = await _saldoHandler.HandleAsync(new GetSaldoQuery(id), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpPost("{id:guid}/cuenta-corriente/nota-debito")]
    [Authorize(Roles = "Administrador,EncargadoCompras")]
    public async Task<IActionResult> RegisterNotaDebito(
        Guid id, [FromBody] RegisterNotaDebitoCommand command, CancellationToken cancellationToken)
    {
        if (id != command.ProveedorId)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _notaDebitoHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpPost("{id:guid}/cuenta-corriente/nota-credito")]
    [Authorize(Roles = "Administrador,EncargadoCompras")]
    public async Task<IActionResult> RegisterNotaCredito(
        Guid id, [FromBody] RegisterNotaCreditoCommand command, CancellationToken cancellationToken)
    {
        if (id != command.ProveedorId)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _notaCreditoHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }
}
