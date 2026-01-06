using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;

namespace Tarot.Api.Controllers;

[Authorize(Policy = "DESIGN_EDIT")]
[ApiController]
[Route("api/v1/[controller]")]
public class SiteSettingsController(IRepository<SiteSetting> settingsRepo) : ControllerBase
{
    private readonly IRepository<SiteSetting> _settingsRepo = settingsRepo;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _settingsRepo.ListAllReadOnlyAsync();
        return Ok(list.Select(s => new
        {
            s.Id,
            s.Key,
            s.Value,
            s.CreatedAt,
            s.UpdatedAt
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var s = await _settingsRepo.GetByIdAsync(id);
        if (s == null) return NotFound();
        return Ok(s);
    }

    [HttpGet("by-key/{key}")]
    public async Task<IActionResult> GetByKey(string key)
    {
        var s = await _settingsRepo.FirstOrDefaultAsync(x => x.Key == key);
        if (s == null) return NotFound();
        return Ok(s);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SiteSettingCreateDto dto)
    {
        var existing = await _settingsRepo.FirstOrDefaultAsync(x => x.Key == dto.Key);
        if (existing != null) return BadRequest("Key exists");
        var s = new SiteSetting
        {
            Key = dto.Key,
            Value = dto.Value,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var created = await _settingsRepo.AddAsync(s);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] SiteSettingUpdateDto dto)
    {
        var s = await _settingsRepo.GetByIdAsync(id);
        if (s == null) return NotFound();
        s.Value = dto.Value;
        s.UpdatedAt = DateTimeOffset.UtcNow;
        await _settingsRepo.UpdateAsync(s);
        return Ok(s);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var s = await _settingsRepo.GetByIdAsync(id);
        if (s == null) return NotFound();
        await _settingsRepo.DeleteAsync(s);
        return NoContent();
    }
}

public class SiteSettingCreateDto
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = "{}";
}

public class SiteSettingUpdateDto
{
    public string Value { get; set; } = "{}";
}
