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
    IRepository<BlockedSlot> blockedSlotRepo,
    IRedisService redis) : ControllerBase
{
    private readonly IRepository<Appointment> _appointmentRepo = appointmentRepo;
    private readonly IRepository<Service> _serviceRepo = serviceRepo;
    private readonly IRepository<BlockedSlot> _blockedSlotRepo = blockedSlotRepo;
    private readonly IRedisService _redis = redis;

    // [Authorize(Policy = "SCHEDULE_MANAGE")]
    // [HttpPost("~/api/v1/admin/slots/block")]
    // public async Task<IActionResult> BlockSlot([FromBody] BlockSlotDto dto)
    // {
    //     if (dto.EndTime <= dto.StartTime)
    //         return BadRequest("EndTime must be after StartTime");
    //
    //     var block = new BlockedSlot
    //     {
    //         StartTime = dto.StartTime,
    //         EndTime = dto.EndTime,
    //         Reason = dto.Reason,
    //         CreatedAt = DateTimeOffset.UtcNow
    //     };
    //
    //     await _blockedSlotRepo.AddAsync(block);
    //     
    //     return Ok(new { Message = "Slot blocked successfully", BlockId = block.Id });
    // }

    // Simplified slots endpoint: requires serviceId and date (YYYY-MM-DD)
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateTime date, [FromQuery] Guid serviceId)
    {
        var cacheKey = $"slots:{date:yyyyMMdd}:{serviceId}";
        var cached = await _redis.GetAsync<List<SlotDto>>(cacheKey);
        if (cached != null) return Ok(cached);

        var service = await _serviceRepo.GetByIdAsync(serviceId);
        if (service == null || !service.IsActive) return BadRequest("Invalid service");

        var startOfDay = new DateTimeOffset(date.Date, TimeSpan.Zero);
        var endOfDay = startOfDay.AddDays(1);

        // Fetch existing appointments
        var appointments = await _appointmentRepo.ListReadOnlyAsync(a => a.StartTime < endOfDay && a.EndTime > startOfDay);
        var booked = appointments
            .Select(a => (a.StartTime, a.EndTime))
            .ToList();

        // Fetch blocked slots
        var blockedSlots = await _blockedSlotRepo.ListReadOnlyAsync(b => b.StartTime < endOfDay && b.EndTime > startOfDay);
        var blocks = blockedSlots
            .Select(b => (b.StartTime, b.EndTime))
            .ToList();

        // Generate slots every DurationMin between business hours 09:00-18:00 (Simplified)
        var businessStart = new DateTimeOffset(date.Date.AddHours(9), TimeSpan.Zero);
        var businessEnd = new DateTimeOffset(date.Date.AddHours(18), TimeSpan.Zero);

        var available = new List<SlotDto>();
        for (var t = businessStart; t.AddMinutes(service.DurationMin) <= businessEnd; t = t.AddMinutes(service.DurationMin))
        {
            var slotEnd = t.AddMinutes(service.DurationMin);
            
            // Check Appointment Overlaps
            var hasAppointment = booked.Any(b => b.StartTime < slotEnd && b.EndTime > t);
            
            // Check Blocked Overlaps
            var isBlocked = blocks.Any(b => b.StartTime < slotEnd && b.EndTime > t);

            if (!hasAppointment && !isBlocked)
                available.Add(new SlotDto { Start = t, End = slotEnd });
        }

        await _redis.SetAsync(cacheKey, available, TimeSpan.FromMinutes(5));
        return Ok(available);
    }
}

public class SlotDto
{
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset End { get; set; }
}

public class BlockSlotDto
{
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string? Reason { get; set; }
}
