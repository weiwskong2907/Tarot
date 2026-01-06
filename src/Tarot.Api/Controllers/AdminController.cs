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
using System.Text.RegularExpressions;

namespace Tarot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/admin")]
public partial class AdminController(
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

        if (_dbContext.Database.IsRelational())
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                appt.Status = AppointmentStatus.Cancelled;
                appt.CancellationReason = request.Reason ?? "Cancelled by Admin";
                await _apptRepo.UpdateAsync(appt);
                var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var log = new AuditLog
                {
                    ActorId = adminId,
                    Action = "AppointmentCancel",
                    Details = JsonSerializer.Serialize(new { AppointmentId = appt.Id, Reason = appt.CancellationReason, AffectedUserId = appt.UserId }),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _dbContext.AuditLogs.Add(log);
                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        else
        {
            appt.Status = AppointmentStatus.Cancelled;
            appt.CancellationReason = request.Reason ?? "Cancelled by Admin";
            await _apptRepo.UpdateAsync(appt);
            var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var log = new AuditLog
            {
                ActorId = adminId,
                Action = "AppointmentCancel",
                Details = JsonSerializer.Serialize(new { AppointmentId = appt.Id, Reason = appt.CancellationReason, AffectedUserId = appt.UserId }),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _dbContext.AuditLogs.Add(log);
            await _dbContext.SaveChangesAsync();
        }

        var user = await _userRepo.GetByIdAsync(appt.UserId);
        if (user != null)
        {
            var useOutbox = (Environment.GetEnvironmentVariable("USE_OUTBOX") ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);
            if (useOutbox)
            {
                var msg = new OutboxMessage
                {
                    Type = "email",
                    Payload = JsonSerializer.Serialize(new { To = user.Email!, Subject = "Appointment Cancelled", Body = $"Your appointment on {appt.StartTime} has been cancelled. Reason: {appt.CancellationReason}", IsHtml = false }),
                    Status = "Pending"
                };
                _dbContext.OutboxMessages.Add(msg);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                try
                {
                    await _emailService.SendEmailAsync(user.Email!, "Appointment Cancelled",
                        $"Your appointment on {appt.StartTime} has been cancelled. Reason: {appt.CancellationReason}");
                }
                catch { }
            }
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
            consultation = new Consultation { AppointmentId = id };
            await _consultationRepo.AddAsync(consultation);
        }

        if (_dbContext.Database.IsRelational())
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                consultation.Reply = request.Message;
                consultation.RepliedAt = DateTime.UtcNow;
                appt.Status = AppointmentStatus.Completed;
                if (appt.User != null && appt.Service != null)
                {
                    await _loyaltyService.AwardPointsForAppointmentAsync(appt.User, appt.Service.Price);
                    await _userRepo.UpdateAsync(appt.User);
                }
                await _apptRepo.UpdateAsync(appt);
                await _consultationRepo.UpdateAsync(consultation);
                var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
                var log = new AuditLog
                {
                    ActorId = adminId,
                    Action = "ConsultationReply",
                    Details = JsonSerializer.Serialize(new { AppointmentId = appt.Id, AffectedUserId = appt.UserId }),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _dbContext.AuditLogs.Add(log);
                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        else
        {
            consultation.Reply = request.Message;
            consultation.RepliedAt = DateTime.UtcNow;
            appt.Status = AppointmentStatus.Completed;
            if (appt.User != null && appt.Service != null)
            {
                await _loyaltyService.AwardPointsForAppointmentAsync(appt.User, appt.Service.Price);
                await _userRepo.UpdateAsync(appt.User);
            }
            await _apptRepo.UpdateAsync(appt);
            await _consultationRepo.UpdateAsync(consultation);
            var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var log = new AuditLog
            {
                ActorId = adminId,
                Action = "ConsultationReply",
                Details = JsonSerializer.Serialize(new { AppointmentId = appt.Id, AffectedUserId = appt.UserId }),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _dbContext.AuditLogs.Add(log);
            await _dbContext.SaveChangesAsync();
        }

        if (appt.User != null && !string.IsNullOrEmpty(appt.User.Email))
        {
            var useOutbox = (Environment.GetEnvironmentVariable("USE_OUTBOX") ?? "false").Equals("true", StringComparison.OrdinalIgnoreCase);
            if (useOutbox)
            {
                var payload = new
                {
                    To = appt.User.Email!,
                    TemplateSlug = "consultation-reply",
                    Model = new
                    {
                        appt.User.UserName,
                        AppointmentTime = appt.StartTime,
                        Reply = request.Message,
                        Link = $"https://tarot-app.com/appointments/{appt.Id}"
                    }
                };
                var msg = new OutboxMessage { Type = "email.template", Payload = JsonSerializer.Serialize(payload), Status = "Pending" };
                _dbContext.OutboxMessages.Add(msg);
                await _dbContext.SaveChangesAsync();
            }
            else
                {
                    try
                    {
                        await _emailService.SendTemplateEmailAsync(appt.User.Email, "consultation-reply", new
                        {
                        appt.User.UserName,
                        AppointmentTime = appt.StartTime,
                        Reply = request.Message,
                        Link = $"https://tarot-app.com/appointments/{appt.Id}"
                        });
                    }
                    catch { }
                }
            }

        return Ok(new { Message = "Reply sent and appointment completed" });
    }

    [Authorize(Policy = "SCHEDULE_MANAGE")]
    [HttpPost("slots/block")]
    public async Task<IActionResult> BlockSlot([FromBody] BlockSlotRequest request)
    {
        var adminId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        
        if (_dbContext.Database.IsRelational())
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var block = new BlockedSlot
                {
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    Reason = request.Reason,
                    CreatedBy = adminId,
                    CreatedAt = DateTime.UtcNow
                };
                await _blockedSlotRepo.AddAsync(block);
                var log = new AuditLog
                {
                    ActorId = adminId,
                    Action = "BlockSlot",
                    Details = JsonSerializer.Serialize(new { Start = request.StartTime, End = request.EndTime, request.Reason }),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                };
                _dbContext.AuditLogs.Add(log);
                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        else
        {
            var block = new BlockedSlot
            {
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Reason = request.Reason,
                CreatedBy = adminId,
                CreatedAt = DateTime.UtcNow
            };
            await _blockedSlotRepo.AddAsync(block);
            var log = new AuditLog
            {
                ActorId = adminId,
                Action = "BlockSlot",
                Details = JsonSerializer.Serialize(new { Start = request.StartTime, End = request.EndTime, request.Reason }),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };
            _dbContext.AuditLogs.Add(log);
            await _dbContext.SaveChangesAsync();
        }
        return Ok(new { Message = "Slot blocked successfully" });
    }

    [Authorize(Policy = "TRASH_MANAGE")]
    [HttpGet("audit")]
    public async Task<IActionResult> GetAudit([FromQuery] Guid? actorId, [FromQuery] string? action, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] string? ip = null, [FromQuery] string? sortBy = "createdAt", [FromQuery] string? sortDir = "desc", [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var q = _dbContext.AuditLogs.AsQueryable();
        if (actorId.HasValue) q = q.Where(x => x.ActorId == actorId.Value);
        if (!string.IsNullOrWhiteSpace(action)) q = q.Where(x => x.Action == action);
        if (from.HasValue) q = q.Where(x => x.CreatedAt >= from.Value);
        if (to.HasValue) q = q.Where(x => x.CreatedAt <= to.Value);
        if (!string.IsNullOrWhiteSpace(ip)) q = q.Where(x => x.IpAddress == ip);
        var desc = string.Equals(sortDir, "desc", StringComparison.OrdinalIgnoreCase);
        var key = (sortBy ?? "createdAt").ToLowerInvariant();
        q = key switch
        {
            "actorid" => desc ? q.OrderByDescending(x => x.ActorId) : q.OrderBy(x => x.ActorId),
            "action" => desc ? q.OrderByDescending(x => x.Action) : q.OrderBy(x => x.Action),
            _ => desc ? q.OrderByDescending(x => x.CreatedAt) : q.OrderBy(x => x.CreatedAt)
        };
        var total = await q.CountAsync();
        var items = await q.Skip(Math.Max(0, (page - 1) * pageSize)).Take(pageSize).ToListAsync();
        return Ok(new
        {
            total,
            page,
            pageSize,
            items = items.Select(l => new { l.Id, l.ActorId, l.Action, Details = MaskDetails(l.Details), l.IpAddress, l.CreatedAt })
        });
    }

    [Authorize(Policy = "TRASH_MANAGE")]
    [HttpPost("outbox/process")]
    public async Task<IActionResult> ProcessOutbox([FromQuery] int take = 50)
    {
        var now = DateTimeOffset.UtcNow;
        var list = await _dbContext.OutboxMessages.Where(x => x.Status == "Pending" && (x.NextAttemptAt == null || x.NextAttemptAt <= now)).OrderBy(x => x.CreatedAt).Take(Math.Max(1, take)).ToListAsync();
        int sent = 0, failed = 0;
        foreach (var m in list)
        {
            try
            {
                var doc = JsonDocument.Parse(m.Payload);
                var root = doc.RootElement;
                if (m.Type == "email")
                {
                    var to = root.GetProperty("To").GetString()!;
                    var subject = root.GetProperty("Subject").GetString()!;
                    var body = root.GetProperty("Body").GetString()!;
                    var isHtml = root.TryGetProperty("IsHtml", out var ih) && ih.ValueKind == JsonValueKind.True;
                    await _emailService.SendEmailAsync(to, subject, body, isHtml);
                }
                else if (m.Type == "email.template")
                {
                    var to = root.GetProperty("To").GetString()!;
                    var slug = root.GetProperty("TemplateSlug").GetString()!;
                    object model = new { };
                    if (root.TryGetProperty("Model", out var me))
                    {
                        model = ToModel(me) ?? new { };
                    }
                    await _emailService.SendTemplateEmailAsync(to, slug, model);
                }
                m.Status = "Sent";
                m.SentAt = DateTimeOffset.UtcNow;
                _dbContext.OutboxMessages.Update(m);
                await _dbContext.SaveChangesAsync();
                sent++;
            }
            catch
            {
                var maxRetriesStr = Environment.GetEnvironmentVariable("OUTBOX_MAX_RETRIES") ?? "5";
                var baseBackoffStr = Environment.GetEnvironmentVariable("OUTBOX_BASE_BACKOFF_SECONDS") ?? "30";
                var maxBackoffStr = Environment.GetEnvironmentVariable("OUTBOX_MAX_BACKOFF_SECONDS") ?? "600";
                var maxRetries = int.TryParse(maxRetriesStr, out var mr) ? Math.Max(1, mr) : 5;
                var baseBackoff = int.TryParse(baseBackoffStr, out var bb) ? Math.Max(5, bb) : 30;
                var maxBackoff = int.TryParse(maxBackoffStr, out var mb) ? Math.Max(baseBackoff, mb) : 600;
                if (m.RetryCount >= maxRetries)
                {
                    m.Status = "Failed";
                }
                else
                {
                    m.RetryCount += 1;
                    var backoff = Math.Min(maxBackoff, (int)(baseBackoff * Math.Pow(2, m.RetryCount - 1)));
                    m.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(backoff);
                    m.Status = "Pending";
                }
                _dbContext.OutboxMessages.Update(m);
                await _dbContext.SaveChangesAsync();
                failed++;
            }
        }
        return Ok(new { processed = list.Count, sent, failed });
    }

    private static string? MaskDetails(string? details)
    {
        if (string.IsNullOrEmpty(details)) return details;
        var masked = MaskingRegexes.Email().Replace(details, m =>
        {
            var u = m.Groups[1].Value;
            var d1 = m.Groups[2].Value;
            var d2 = m.Groups[3].Value;
            var um = u.Length <= 2
                ? new string('*', u.Length)
                : string.Concat(u.AsSpan(0, 2), new string('*', Math.Max(0, u.Length - 2)));
            var d1m = d1.Length <= 2
                ? new string('*', d1.Length)
                : string.Concat(d1.AsSpan(0, 2), new string('*', Math.Max(0, d1.Length - 2)));
            return $"{um}@{d1m}.{d2}";
        });
        masked = MaskingRegexes.LongNumbers().Replace(masked, m => new string('*', m.Value.Length));
        return masked;
    }

    private static partial class MaskingRegexes
    {
        [GeneratedRegex(@"([A-Za-z0-9_.+\-]+)@([A-Za-z0-9\-]+)\.([A-Za-z0-9\-.]+)", RegexOptions.CultureInvariant)]
        public static partial Regex Email();

        [GeneratedRegex(@"\b[0-9]{16,}\b", RegexOptions.CultureInvariant)]
        public static partial Regex LongNumbers();
    }

    private static object? ToModel(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var p in el.EnumerateObject())
                {
                    dict[p.Name] = ToModel(p.Value);
                }
                IDictionary<string, object?> exp = new System.Dynamic.ExpandoObject();
                foreach (var kv in dict) exp[kv.Key] = kv.Value;
                return exp;
            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var i in el.EnumerateArray()) list.Add(ToModel(i));
                return list;
            case JsonValueKind.String:
                return el.GetString();
            case JsonValueKind.Number:
                if (el.TryGetInt64(out var l)) return l;
                if (el.TryGetDouble(out var d)) return d;
                return el.GetRawText();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
            default:
                return null;
        }
    }

    [Authorize(Policy = "TRASH_MANAGE")]
    [HttpGet("outbox/metrics")]
    public async Task<IActionResult> GetOutboxMetrics()
    {
        var now = DateTimeOffset.UtcNow;
        var pending = await _dbContext.OutboxMessages.CountAsync(x => x.Status == "Pending");
        var pendingReady = await _dbContext.OutboxMessages.CountAsync(x => x.Status == "Pending" && (x.NextAttemptAt == null || x.NextAttemptAt <= now));
        var failed24h = await _dbContext.OutboxMessages.CountAsync(x => x.Status == "Failed" && x.UpdatedAt != null && x.UpdatedAt >= now.AddHours(-24));
        var sent24h = await _dbContext.OutboxMessages.CountAsync(x => x.Status == "Sent" && x.SentAt != null && x.SentAt >= now.AddHours(-24));
        var oldestPending = await _dbContext.OutboxMessages.Where(x => x.Status == "Pending").OrderBy(x => x.CreatedAt).Select(x => x.CreatedAt).FirstOrDefaultAsync();
        return Ok(new
        {
            pending,
            pendingReady,
            failed24h,
            sent24h,
            oldestPendingAgeMinutes = oldestPending == default ? 0 : (int)(now - oldestPending).TotalMinutes
        });
    }

    [Authorize(Policy = "TRASH_MANAGE")]
    [HttpGet("outbox/recent")]
    public async Task<IActionResult> GetOutboxRecent([FromQuery] string? status = null, [FromQuery] int take = 20)
    {
        var q = _dbContext.OutboxMessages.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
        var items = await q.OrderByDescending(x => x.CreatedAt).Take(Math.Max(1, take)).Select(x => new
        {
            x.Id,
            x.Type,
            x.Status,
            x.RetryCount,
            x.NextAttemptAt,
            x.SentAt,
            x.CreatedAt
        }).ToListAsync();
        return Ok(items);
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

    [Authorize(Policy = "TRASH_MANAGE")]
    [HttpGet("trash")]
    public async Task<IActionResult> GetTrash([FromQuery] string entity)
    {
        switch ((entity ?? "").ToLowerInvariant())
        {
            case "cards":
                {
                    var list = await _dbContext.Cards.IgnoreQueryFilters().Where(x => x.DeletedAt != null).ToListAsync();
                    return Ok(list.Select(x => new { x.Id, x.Name, x.DeletedAt }));
                }
            case "services":
                {
                    var list = await _dbContext.Services.IgnoreQueryFilters().Where(x => x.DeletedAt != null).ToListAsync();
                    return Ok(list.Select(x => new { x.Id, x.Name, x.DeletedAt }));
                }
            case "appointments":
                {
                    var list = await _dbContext.Appointments.IgnoreQueryFilters().Where(x => x.DeletedAt != null).ToListAsync();
                    return Ok(list.Select(x => new { x.Id, x.UserId, x.ServiceId, x.DeletedAt }));
                }
            case "consultations":
                {
                    var list = await _dbContext.Consultations.IgnoreQueryFilters().Where(x => x.DeletedAt != null).ToListAsync();
                    return Ok(list.Select(x => new { x.Id, x.AppointmentId, x.DeletedAt }));
                }
            case "blogposts":
                {
                    var list = await _dbContext.BlogPosts.IgnoreQueryFilters().Where(x => x.DeletedAt != null).ToListAsync();
                    return Ok(list.Select(x => new { x.Id, x.Slug, x.DeletedAt }));
                }
            case "sitesettings":
                {
                    var list = await _dbContext.SiteSettings.IgnoreQueryFilters().Where(x => x.DeletedAt != null).ToListAsync();
                    return Ok(list.Select(x => new { x.Id, x.Key, x.DeletedAt }));
                }
            case "emailtemplates":
                {
                    var list = await _dbContext.EmailTemplates.IgnoreQueryFilters().Where(x => x.DeletedAt != null).ToListAsync();
                    return Ok(list.Select(x => new { x.Id, x.Slug, x.DeletedAt }));
                }
            case "contactmessages":
                {
                    var list = await _dbContext.ContactMessages.IgnoreQueryFilters().Where(x => x.DeletedAt != null).ToListAsync();
                    return Ok(list.Select(x => new { x.Id, x.Email, x.DeletedAt }));
                }
        }
        return BadRequest("Unknown entity");
    }

    [Authorize(Policy = "TRASH_MANAGE")]
    [HttpPost("trash/restore")]
    public async Task<IActionResult> Restore([FromBody] RestoreRequest request)
    {
        var entity = (request.Entity ?? "").ToLowerInvariant();
        if (request.Id == Guid.Empty) return BadRequest("Invalid id");
        switch (entity)
        {
            case "cards":
                {
                    var x = await _dbContext.Cards.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == request.Id);
                    if (x == null) return NotFound();
                    x.DeletedAt = null;
                    await _dbContext.SaveChangesAsync();
                    return Ok(new { Message = "Restored", x.Id });
                }
            case "services":
                {
                    var x = await _dbContext.Services.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == request.Id);
                    if (x == null) return NotFound();
                    x.DeletedAt = null;
                    await _dbContext.SaveChangesAsync();
                    return Ok(new { Message = "Restored", x.Id });
                }
            case "appointments":
                {
                    var x = await _dbContext.Appointments.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == request.Id);
                    if (x == null) return NotFound();
                    x.DeletedAt = null;
                    await _dbContext.SaveChangesAsync();
                    return Ok(new { Message = "Restored", x.Id });
                }
            case "consultations":
                {
                    var x = await _dbContext.Consultations.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == request.Id);
                    if (x == null) return NotFound();
                    x.DeletedAt = null;
                    await _dbContext.SaveChangesAsync();
                    return Ok(new { Message = "Restored", x.Id });
                }
            case "blogposts":
                {
                    var x = await _dbContext.BlogPosts.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == request.Id);
                    if (x == null) return NotFound();
                    x.DeletedAt = null;
                    await _dbContext.SaveChangesAsync();
                    return Ok(new { Message = "Restored", x.Id });
                }
            case "sitesettings":
                {
                    var x = await _dbContext.SiteSettings.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == request.Id);
                    if (x == null) return NotFound();
                    x.DeletedAt = null;
                    await _dbContext.SaveChangesAsync();
                    return Ok(new { Message = "Restored", x.Id });
                }
            case "emailtemplates":
                {
                    var x = await _dbContext.EmailTemplates.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == request.Id);
                    if (x == null) return NotFound();
                    x.DeletedAt = null;
                    await _dbContext.SaveChangesAsync();
                    return Ok(new { Message = "Restored", x.Id });
                }
            case "contactmessages":
                {
                    var x = await _dbContext.ContactMessages.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == request.Id);
                    if (x == null) return NotFound();
                    x.DeletedAt = null;
                    await _dbContext.SaveChangesAsync();
                    return Ok(new { Message = "Restored", x.Id });
                }
        }
        return BadRequest("Unknown entity");
    }
}

public class CancelRequest { public string? Reason { get; set; } }
public class ReplyRequest { public string Message { get; set; } = string.Empty; }
public class BlockSlotRequest { public DateTimeOffset StartTime { get; set; } public DateTimeOffset EndTime { get; set; } public string? Reason { get; set; } }
public class CreateStaffRequest { public string Email { get; set; } = string.Empty; public string FullName { get; set; } = string.Empty; public string Password { get; set; } = string.Empty; public List<string>? Permissions { get; set; } }
public class UpdatePermissionsRequest { public List<string>? Permissions { get; set; } }
public class RestoreRequest { public string Entity { get; set; } = string.Empty; public Guid Id { get; set; } }
