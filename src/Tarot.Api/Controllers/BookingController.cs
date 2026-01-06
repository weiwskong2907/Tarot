using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tarot.Api.ViewModels;
using Tarot.Infrastructure.Data;
using Tarot.Core.Entities;
using System.Security.Claims;

namespace Tarot.Api.Controllers;

[Authorize]
public class BookingController : Controller
{
    private readonly AppDbContext _db;

    public BookingController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? serviceId)
    {
        var services = await _db.Services
            .AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => new ServiceViewModel
            {
                Name = s.Name,
                Price = s.Price,
                Description = s.Description ?? string.Empty,
                Duration = $"{s.DurationMin} min"
            })
            .ToListAsync();

        var model = new BookingViewModel
        {
            AvailableServices = services,
            Date = DateTime.Today.AddDays(1)
        };
        
        if (serviceId.HasValue)
        {
            model.ServiceId = serviceId.Value;
        }

        return View(model);
    }

    [HttpGet]
    public IActionResult GetSlots(DateTime date)
    {
        // Mock logic for slots - in real app, check DB for existing appointments
        // and admin blocks.
        var slots = new List<SlotViewModel>();
        var start = new TimeSpan(9, 0, 0); // 9 AM
        var end = new TimeSpan(17, 0, 0); // 5 PM

        for (var time = start; time < end; time = time.Add(TimeSpan.FromHours(1)))
        {
            // Randomly mark some as unavailable for demo
            slots.Add(new SlotViewModel 
            { 
                Time = time.ToString(@"hh\:mm"), 
                IsAvailable = new Random().Next(0, 10) > 2 
            });
        }

        return Json(slots);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingViewModel model)
    {
        if (ModelState.IsValid)
        {
             // Store in TempData or create Pending Appointment directly
             // For simplicity, we'll create the appointment directly
             
             var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
             // Find Service
             var service = await _db.Services.FindAsync(model.ServiceId);
             
             if (service == null)
             {
                 ModelState.AddModelError("", "Service not found.");
                 return View(model);
             }
             
             // ... Logic to save appointment ...
             
             return RedirectToAction(nameof(Success));
        }
        return View(model);
    }

    public IActionResult Success()
    {
        return View();
    }
}
