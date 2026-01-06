using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tarot.Core.Interfaces;

namespace Tarot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    private readonly IAnalyticsService _analyticsService = analyticsService;

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var history = await _analyticsService.GetUserDrawHistoryAsync(userId, from, to);
        return Ok(history);
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var stats = await _analyticsService.GetUserCardStatsAsync(userId);
        return Ok(stats);
    }
}
