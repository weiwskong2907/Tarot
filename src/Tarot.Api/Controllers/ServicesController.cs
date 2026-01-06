using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tarot.Infrastructure.Data;
using Tarot.Api.ViewModels;

namespace Tarot.Api.Controllers;

public class ServicesController : Controller
{
    private readonly AppDbContext _db;

    public ServicesController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var services = await _db.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new ServiceViewModel
            {
                Name = s.Name,
                Description = s.Description ?? "No description available.",
                Price = s.Price,
                Duration = $"{s.DurationMin} min"
            })
            .ToListAsync();

        return View(services);
    }
}
