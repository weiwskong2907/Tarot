using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tarot.Infrastructure.Data;

namespace Tarot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthCheckController : ControllerBase
{
    private readonly AppDbContext _db;

    public HealthCheckController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var dbHealthy = await _db.Database.CanConnectAsync();
        var result = new
        {
            status = "OK",
            database = dbHealthy ? "Healthy" : "Unreachable",
            time = DateTimeOffset.UtcNow
        };
        return Ok(result);
    }
}

