using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;

namespace Tarot.Api.Controllers;

[Authorize(Policy = "DESIGN_EDIT")]
[ApiController]
[Route("api/v1/[controller]")]
public class SettingsController(IRepository<SiteSetting> settingsRepo) : ControllerBase
{
    private readonly IRepository<SiteSetting> _settingsRepo = settingsRepo;

    [HttpPut("design")]
    public async Task<IActionResult> UpdateDesign([FromBody] DesignUpdateDto dto)
    {
        var all = await _settingsRepo.ListAllAsync();
        var setting = all.FirstOrDefault(s => s.Key == "design_config");
        if (setting == null)
        {
            setting = new SiteSetting
            {
                Key = "design_config",
                Value = dto.JsonConfig,
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _settingsRepo.AddAsync(setting);
        }
        else
        {
            setting.Value = dto.JsonConfig;
            setting.UpdatedAt = DateTimeOffset.UtcNow;
            await _settingsRepo.UpdateAsync(setting);
        }
        return Ok(new { Message = "Design settings updated" });
    }
}

public class DesignUpdateDto
{
    public string JsonConfig { get; set; } = "{}";
}
