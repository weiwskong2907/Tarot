using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;

namespace Tarot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class LoyaltyController(IRepository<AppUser> userRepo, ILoyaltyService loyaltyService) : ControllerBase
{
    private readonly IRepository<AppUser> _userRepo = userRepo;
    private readonly ILoyaltyService _loyaltyService = loyaltyService;

    [HttpGet("points")]
    public async Task<IActionResult> GetPoints()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
        
        var userId = Guid.Parse(userIdStr);
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return NotFound();

        return Ok(new
        {
            Points = user.LoyaltyPoints,
            AppointmentCount = user.AppointmentCount,
            Level = _loyaltyService.GetLoyaltyLevel(user.AppointmentCount)
        });
    }
}
