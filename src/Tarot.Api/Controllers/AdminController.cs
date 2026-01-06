using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tarot.Core.Entities;
using Tarot.Core.Enums;
using Tarot.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using Tarot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Tarot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/admin")]
public class AdminController(
    IRepository<Appointment> apptRepo,
    IRepository<BlockedSlot> blockedSlotRepo,
    IRepository<Consultation> consultationRepo,
    IRepository<AppUser> userRepo,
    IEmailService emailService,
    ILoyaltyService loyaltyService,
    UserManager<AppUser> userManager,
    AppDbContext dbContext
) : ControllerBase
{
    private readonly IRepository<Appointment> _apptRepo = apptRepo;
    private readonly IRepository<BlockedSlot> _blockedSlotRepo = blockedSlotRepo;
    private readonly IRepository<Consultation> _consultationRepo = consultationRepo;
    private readonly IRepository<AppUser> _userRepo = userRepo;
    private readonly IEmailService _emailService = emailService;
    private readonly ILoyaltyService _loyaltyService = loyaltyService;
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly AppDbContext _dbContext = dbContext;

    [Authorize(Policy = "SCHEDULE_MANAGE")]
    [HttpPost("appointments/{id}/cancel")]
    public async Task<IActionResult> CancelAppointment(Guid id, [FromBody] CancelRequest request)
    {
        var appt = await _apptRepo.GetByIdAsync(id);
        if (appt == null) return NotFound();

        appt.Status = AppointmentStatus.Cancelled;
        appt.CancellationReason = request.Reason ?? "Cancelled by Admin";
        await _apptRepo.UpdateAsync(appt);

        var user = await _userRepo.GetByIdAsync(appt.UserId);
        if (user != null)
        {
            await _emailService.SendEmailAsync(user.Email!, "Appointment Cancelled", 
                $"Your appointment on {appt.StartTime} has been cancelled. Reason: {appt.CancellationReason}");
        }

        return Ok(new { Message = "Appointment cancelled" });
    }

    [Authorize(Policy = "CONSULTATION_REPLY")]
    [HttpPost("appointments/{id}/reply")]
    public async Task<IActionResult> ReplyConsultation(Guid id, [FromBody] ReplyRequest request)
    {
        var appt = await _dbContext.Appointments
            .Include(a => a.User)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (appt == null) return NotFound();

        var consultation = await _consultationRepo.FirstOrDefaultAsync(c => c.AppointmentId == id);
        
        if (consultation == null)
        {
            // Create if missing (edge case)
            consultation = new Consultation { AppointmentId = id };
            await _consultationRepo.AddAsync(consultation);
        }

        consultation.Reply = request.Message;
        consultation.RepliedAt = DateTime.UtcNow;
        // In real impl, handle images
        
        appt.Status = AppointmentStatus.Completed; // Mark as completed after reply
        
        // Award Loyalty Points
        if (appt.User != null && appt.Service != null)
        {
            await _loyaltyService.AwardPointsForAppointmentAsync(appt.User, appt.Service.Price);
        }

        await _apptRepo.UpdateAsync(appt);
        await _consultationRepo.UpdateAsync(consultation);

        return Ok(new { Message = "Reply sent and appointment completed" });
    }

    [Authorize(Policy = "SCHEDULE_MANAGE")]
    [HttpPost("slots/block")]
    public async Task<IActionResult> BlockSlot([FromBody] BlockSlotRequest request)
    {
        var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        
        var block = new BlockedSlot
        {
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Reason = request.Reason,
            CreatedBy = adminId,
            CreatedAt = DateTime.UtcNow
        };

        await _blockedSlotRepo.AddAsync(block);
        return Ok(new { Message = "Slot blocked successfully" });
    }

    [Authorize(Policy = "KNOWLEDGE_EDIT")]
    [HttpPost("cards/reload")]
    public async Task<IActionResult> ReloadCards()
    {
        var success = await DbInitializer.ReloadCardsAsync(_dbContext);
        if (success)
            return Ok(new { Message = "Cards reloaded successfully." });
        
        return Ok(new { Message = "No card configuration found. No changes made." });
    }

    [Authorize(Policy = "DESIGN_EDIT")]
    [HttpPost("staff")]
    public async Task<IActionResult> CreateStaff([FromBody] CreateStaffRequest request)
    {
        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing != null)
        {
            return BadRequest("User already exists");
        }

        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            FullName = request.FullName,
            CreatedAt = DateTimeOffset.UtcNow,
            Permissions = JsonSerializer.Serialize(request.Permissions ?? [])
        };
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }
        return Ok(new { Message = "Staff created", userId = user.Id, permissions = request.Permissions });
    }

    [Authorize(Policy = "DESIGN_EDIT")]
    [HttpPut("staff/{id:guid}/permissions")]
    public async Task<IActionResult> UpdateStaffPermissions(Guid id, [FromBody] UpdatePermissionsRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null) return NotFound();
        user.Permissions = JsonSerializer.Serialize(request.Permissions ?? []);
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }
        return Ok(new { Message = "Permissions updated", userId = id, permissions = request.Permissions });
    }
}

public class CancelRequest { public string? Reason { get; set; } }
public class ReplyRequest { public string Message { get; set; } = string.Empty; }
public class BlockSlotRequest { public DateTimeOffset StartTime { get; set; } public DateTimeOffset EndTime { get; set; } public string? Reason { get; set; } }
public class CreateStaffRequest { public string Email { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public string Password { get; set; } = string.Empty; public List<string>? Permissions { get; set; } }
public class UpdatePermissionsRequest { public List<string>? Permissions { get; set; } }
