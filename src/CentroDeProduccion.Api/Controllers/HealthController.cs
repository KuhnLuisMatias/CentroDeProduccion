using CentroDeProduccion.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CentroDeProduccion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            var canConnect = await _db.Database.CanConnectAsync();
            var dbName = _db.Database.GetDbConnection().Database;

            return Ok(new
            {
                status = "ok",
                timestamp = DateTime.UtcNow,
                database = new
                {
                    connected = canConnect,
                    name = dbName,
                    provider = "SQL Server"
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                status = "error",
                message = ex.Message
            });
        }
    }
}
