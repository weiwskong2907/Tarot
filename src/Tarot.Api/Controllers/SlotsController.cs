using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;

namespace Tarot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class SlotsController(
    IRepository<Appointment> appointmentRepo, 
    IRepository<Service> serviceRepo,
    IRepository<BlockedSlot> blockedSlotRepo) : ControllerBase
{
    private readonly IRepository<Appointment> _appointmentRepo = appointmentRepo;
    private readonly IRepository<Service> _serviceRepo = serviceRepo;
    private readonly IRepository<BlockedSlot> _blockedSlotRepo = blockedSlotRepo;

    // Simplified slots endpoint: requires serviceId and date (YYYY-MM-DD)
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime date, [FromQuery] Guid serviceId)
    {
        var service = await _serviceRepo.GetByIdAsync(serviceId);
        if (service == null || !service.IsActive) return BadRequest("Invalid service");

        var startOfDay = new DateTimeOffset(date.Date, TimeSpan.Zero);
        var endOfDay = startOfDay.AddDays(1);

        // Fetch existing appointments
        var appointments = await _appointmentRepo.ListAsync(a => a.StartTime < endOfDay && a.EndTime > startOfDay);
        var booked = appointments
            .Select(a => (a.StartTime, a.EndTime))
            .ToList();

        // Fetch blocked slots
        var blockedSlots = await _blockedSlotRepo.ListAsync(b => b.StartTime < endOfDay && b.EndTime > startOfDay);
        var blocks = blockedSlots
            .Select(b => (b.StartTime, b.EndTime))
            .ToList();

        // Generate slots every DurationMin between business hours 09:00-18:00 (Simplified)
        var businessStart = new DateTimeOffset(date.Date.AddHours(9), TimeSpan.Zero);
        var businessEnd = new DateTimeOffset(date.Date.AddHours(18), TimeSpan.Zero);

        var available = new List<DateTimeOffset>();
        for (var t = businessStart; t.AddMinutes(service.DurationMin) <= businessEnd; t = t.AddMinutes(service.DurationMin))
        {
            var slotEnd = t.AddMinutes(service.DurationMin);
            
            // Check Appointment Overlaps
            var hasAppointment = booked.Any(b => b.StartTime < slotEnd && b.EndTime > t);
            
            // Check Blocked Overlaps
            var isBlocked = blocks.Any(b => b.StartTime < slotEnd && b.EndTime > t);

            if (!hasAppointment && !isBlocked)
                available.Add(t);
        }

        return Ok(available.Select(x => new { start = x, end = x.AddMinutes(service.DurationMin) }));
    }
}
