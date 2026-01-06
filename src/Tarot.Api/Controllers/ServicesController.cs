using Microsoft.AspNetCore.Mvc;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;

namespace Tarot.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ServicesController(IRepository<Service> serviceRepo) : ControllerBase
{
    private readonly IRepository<Service> _serviceRepo = serviceRepo;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        q = (q ?? "").Trim();
        int skip = Math.Max(0, (page - 1) * pageSize);
        var total = await _serviceRepo.CountAsync(s => string.IsNullOrWhiteSpace(q) || (s.Name ?? "").Contains(q, StringComparison.OrdinalIgnoreCase));
        var items = await _serviceRepo.ListAsync(s => string.IsNullOrWhiteSpace(q) || (s.Name ?? "").Contains(q, StringComparison.OrdinalIgnoreCase), skip, pageSize);
        return Ok(new
        {
            total,
            page,
            pageSize,
            items = items.Select(s => new
            {
                s.Id,
                s.Name,
                s.Price,
                s.DurationMin,
                s.IsActive
            })
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var service = await _serviceRepo.GetByIdAsync(id);
        if (service == null) return NotFound();
        return Ok(new
        {
            service.Id,
            service.Name,
            service.Price,
            service.DurationMin,
            service.IsActive
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ServiceCreateDto dto)
    {
        var service = new Service
        {
            Name = dto.Name,
            Price = dto.Price,
            DurationMin = dto.DurationMin,
            IsActive = dto.IsActive,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var created = await _serviceRepo.AddAsync(service);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, new
        {
            created.Id,
            created.Name,
            created.Price,
            created.DurationMin,
            created.IsActive
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] ServiceUpdateDto dto)
    {
        var service = await _serviceRepo.GetByIdAsync(id);
        if (service == null) return NotFound();
        service.Name = dto.Name;
        service.Price = dto.Price;
        service.DurationMin = dto.DurationMin;
        service.IsActive = dto.IsActive;
        service.UpdatedAt = DateTimeOffset.UtcNow;
        await _serviceRepo.UpdateAsync(service);
        return Ok(new
        {
            service.Id,
            service.Name,
            service.Price,
            service.DurationMin,
            service.IsActive
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var service = await _serviceRepo.GetByIdAsync(id);
        if (service == null) return NotFound();
        await _serviceRepo.DeleteAsync(service);
        return NoContent();
    }
}

public class ServiceCreateDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMin { get; set; }
    public bool IsActive { get; set; } = true;
}

public class ServiceUpdateDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMin { get; set; }
    public bool IsActive { get; set; } = true;
}
