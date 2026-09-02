using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.CuentaCorrienteBar.Commands.RegisterCompensacion;
using CentroDeProduccion.Application.Features.CuentaCorrienteBar.Commands.RegisterNotaCredito;
using CentroDeProduccion.Application.Features.CuentaCorrienteBar.Commands.RegisterNotaDebito;
using CentroDeProduccion.Application.Features.CuentaCorrienteBar.Queries.GetEstadoCuenta;
using CentroDeProduccion.Application.Features.CuentaCorrienteBar.Queries.GetSaldo;
using CentroDeProduccion.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/bares/{barId:guid}/cuenta-corriente")]
[Authorize(Roles = "Administrador,EncargadoVentas")]
public class CuentaCorrienteBarController : ControllerBase
{
    private readonly GetEstadoCuentaQueryHandler _getEstadoCuentaHandler;
    private readonly GetSaldoQueryHandler _getSaldoHandler;
    private readonly RegisterNotaCreditoCommandHandler _registrarNotaCreditoHandler;
    private readonly RegisterNotaDebitoCommandHandler _registrarNotaDebitoHandler;
    private readonly RegisterCompensacionCommandHandler _registrarCompensacionHandler;

    public CuentaCorrienteBarController(
        GetEstadoCuentaQueryHandler getEstadoCuentaHandler,
        GetSaldoQueryHandler getSaldoHandler,
        RegisterNotaCreditoCommandHandler registrarNotaCreditoHandler,
        RegisterNotaDebitoCommandHandler registrarNotaDebitoHandler,
        RegisterCompensacionCommandHandler registrarCompensacionHandler)
    {
        _getEstadoCuentaHandler = getEstadoCuentaHandler;
        _getSaldoHandler = getSaldoHandler;
        _registrarNotaCreditoHandler = registrarNotaCreditoHandler;
        _registrarNotaDebitoHandler = registrarNotaDebitoHandler;
        _registrarCompensacionHandler = registrarCompensacionHandler;
    }

    [HttpGet]
    public async Task<IActionResult> GetEstadoCuenta(
        Guid barId,
        [FromQuery] TipoMovimientoCtaCteBar? tipo,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        CancellationToken cancellationToken)
    {
        var result = await _getEstadoCuentaHandler.HandleAsync(
            new GetEstadoCuentaQuery(barId, tipo, fechaDesde, fechaHasta), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpGet("saldo")]
    public async Task<IActionResult> GetSaldo(Guid barId, CancellationToken cancellationToken)
    {
        var result = await _getSaldoHandler.HandleAsync(new GetSaldoQuery(barId), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpPost("nota-credito")]
    public async Task<IActionResult> RegisterNotaCredito(Guid barId, [FromBody] RegisterNotaCreditoCommand command, CancellationToken cancellationToken)
    {
        if (barId != command.BarId)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _registrarNotaCreditoHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpPost("nota-debito")]
    public async Task<IActionResult> RegisterNotaDebito(Guid barId, [FromBody] RegisterNotaDebitoCommand command, CancellationToken cancellationToken)
    {
        if (barId != command.BarId)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _registrarNotaDebitoHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpPost("compensacion")]
    public async Task<IActionResult> RegisterCompensacion(Guid barId, [FromBody] RegisterCompensacionCommand command, CancellationToken cancellationToken)
    {
        if (barId != command.BarId)
            return BadRequest("El ID de la URL no coincide con el ID del cuerpo");

        var result = await _registrarCompensacionHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }
}