using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Features.PagosBar.Commands.CreatePagoBar;
using CentroDeProduccion.Application.Features.PagosBar.Queries.GetPagoBarById;
using CentroDeProduccion.Application.Features.PagosBar.Queries.GetPagoBarList;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/pagos-bar")]
[Authorize(Roles = "Administrador,EncargadoVentas")]
public class PagosBarController : ControllerBase
{
    private readonly CreatePagoBarCommandHandler _createHandler;
    private readonly GetPagoBarByIdQueryHandler _getByIdHandler;
    private readonly GetPagoBarListQueryHandler _getListHandler;

    public PagosBarController(
        CreatePagoBarCommandHandler createHandler,
        GetPagoBarByIdQueryHandler getByIdHandler,
        GetPagoBarListQueryHandler getListHandler)
    {
        _createHandler = createHandler;
        _getByIdHandler = getByIdHandler;
        _getListHandler = getListHandler;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePagoBarCommand command, CancellationToken cancellationToken)
    {
        var result = await _createHandler.HandleAsync(command, cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] Guid? barId,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        CancellationToken cancellationToken)
    {
        var result = await _getListHandler.HandleAsync(
            new GetPagoBarListQuery(barId, fechaDesde, fechaHasta), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _getByIdHandler.HandleAsync(new GetPagoBarByIdQuery(id), cancellationToken);
        return result.ToActionResult(this, response => Ok(response));
    }
}