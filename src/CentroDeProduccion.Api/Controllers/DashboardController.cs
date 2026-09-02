using CentroDeProduccion.Api.Extensions;
using CentroDeProduccion.Application.Authorization;
using CentroDeProduccion.Application.Features.Dashboard.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = AuthorizationPolicies.CanViewDashboard)]
public class DashboardController : ControllerBase
{
    private readonly GetDashboardQueryHandler _getDashboardHandler;
    private readonly GetDashboardChartsQueryHandler _getDashboardChartsHandler;

    public DashboardController(
        GetDashboardQueryHandler getDashboardHandler,
        GetDashboardChartsQueryHandler getDashboardChartsHandler)
    {
        _getDashboardHandler = getDashboardHandler;
        _getDashboardChartsHandler = getDashboardChartsHandler;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken = default)
    {
        var result = await _getDashboardHandler.HandleAsync(new GetDashboardQuery(), cancellationToken);
        Response.SetNoCache();
        return result.ToActionResult(this);
    }

    [HttpGet("charts")]
    public async Task<IActionResult> GetCharts(CancellationToken cancellationToken = default)
    {
        var result = await _getDashboardChartsHandler.HandleAsync(new GetDashboardChartsQuery(), cancellationToken);
        Response.SetNoCache();
        return result.ToActionResult(this);
    }
}
