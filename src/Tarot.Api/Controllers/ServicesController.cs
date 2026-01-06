using Microsoft.AspNetCore.Mvc;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;

namespace Tarot.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly IRepository<Service> _serviceRepo;

    public ServicesController(IRepository<Service> serviceRepo)
    {
        _serviceRepo = serviceRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var services = await _serviceRepo.ListAllAsync();
        return Ok(services.Select(s => new
        {
            s.Id,
            s.Name,
            s.Price,
            s.DurationMin,
            s.IsActive
        }));
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
}

