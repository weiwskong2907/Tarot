using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tarot.Api.Dtos;
using Tarot.Core.Interfaces;
using Tarot.Core.Entities;
using System.Text.Json;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Microsoft.Extensions.Options;
using Tarot.Core.Settings;

namespace Tarot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPaymentService _paymentService;
    private readonly IRepository<Appointment> _appointmentRepo;
    private readonly IRepository<Service> _serviceRepo;
    private readonly IRepository<Consultation> _consultationRepo;
    private readonly AppSettings _settings;

    public AppointmentsController(
        IAppointmentService appointmentService, 
        IPaymentService paymentService, 
        IRepository<Appointment> appointmentRepo, 
        IRepository<Service> serviceRepo, 
        IRepository<Consultation> consultationRepo,
        IOptions<AppSettings> settings)
    {
        _appointmentService = appointmentService;
        _paymentService = paymentService;
        _appointmentRepo = appointmentRepo;
        _serviceRepo = serviceRepo;
        _consultationRepo = consultationRepo;
        _settings = settings.Value;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        
        var appointment = await _appointmentService.CreateAppointmentAsync(userId, dto.ServiceId, dto.StartTime);
        
        // Allow environment variable override for testing
        var enablePayment = _settings.EnablePayment;
        var envEnable = Environment.GetEnvironmentVariable("ENABLE_PAYMENT");
        if (envEnable != null && bool.TryParse(envEnable, out var e))
        {
            enablePayment = e;
        }

        if (!enablePayment)
        {
            appointment.Status = Core.Enums.AppointmentStatus.Confirmed;
            appointment.PaymentStatus = Core.Enums.PaymentStatus.Skipped;
            await _appointmentRepo.UpdateAsync(appointment);
        }
        else
        {
            if (appointment.Price > 0)
            {
                var paid = await _paymentService.ProcessPaymentAsync(userId, appointment.Price);
                if (!paid)
                {
                    return BadRequest("Payment failed");
                }
                appointment.Status = Core.Enums.AppointmentStatus.Confirmed;
                appointment.PaymentStatus = Core.Enums.PaymentStatus.Paid;
                await _appointmentRepo.UpdateAsync(appointment);
            }
        }
        
        return Ok(new AppointmentDto
        {
            Id = appointment.Id,
            ServiceId = appointment.ServiceId,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            Status = appointment.Status.ToString(),
            Price = appointment.Price
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetMyAppointments()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var appointments = await _appointmentService.GetUserAppointmentsAsync(userId);
        
        var dtos = appointments.Select(a => new AppointmentDto
        {
            Id = a.Id,
            ServiceId = a.ServiceId,
            StartTime = a.StartTime,
            EndTime = a.EndTime,
            Status = a.Status.ToString(),
            Price = a.Price
        });

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var appointment = await _appointmentService.GetAppointmentByIdAsync(id);
        if (appointment == null || appointment.UserId != userId)
            return NotFound();

        return Ok(new AppointmentDto
        {
            Id = appointment.Id,
            ServiceId = appointment.ServiceId,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            Status = appointment.Status.ToString(),
            Price = appointment.Price,
            CancellationReason = appointment.CancellationReason
        });
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelAppointmentDto? dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var result = await _appointmentService.CancelAppointmentAsync(id, userId, dto?.Reason);

        if (!result)
            return BadRequest("Cannot cancel appointment");

        return Ok(new { Message = "Appointment cancelled" });
    }

    [HttpPost("{id}/reschedule")]
    public async Task<IActionResult> Reschedule(Guid id, [FromBody] RescheduleAppointmentDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        try
        {
            var appt = await _appointmentService.RescheduleAppointmentAsync(id, userId, dto.NewStartTime);
            return Ok(new { Message = "Rescheduled successfully", NewTime = appt.StartTime });
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{id}/consultation")]
    public async Task<IActionResult> SubmitConsultation(Guid id, [FromBody] ConsultationMessageDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var appt = await _appointmentRepo.GetByIdAsync(id);
        if (appt == null || appt.UserId != userId) return NotFound();

        var consultation = (await _consultationRepo.ListAsync(c => c.AppointmentId == id)).FirstOrDefault();
        if (consultation == null)
        {
            consultation = new Consultation { AppointmentId = id, CreatedAt = DateTimeOffset.UtcNow };
            if (dto.ImageUrls != null && dto.ImageUrls.Any())
            {
                consultation.UserImages = JsonSerializer.Serialize(dto.ImageUrls);
            }
            consultation.Question = dto.Message;
            await _consultationRepo.AddAsync(consultation);
        }
        else
        {
            consultation.Question = dto.Message;
            if (dto.ImageUrls != null && dto.ImageUrls.Any())
            {
                consultation.UserImages = JsonSerializer.Serialize(dto.ImageUrls);
            }
            consultation.UpdatedAt = DateTimeOffset.UtcNow;
            await _consultationRepo.UpdateAsync(consultation);
        }

        // Update status to InProgress if Confirmed
        if (appt.Status == Core.Enums.AppointmentStatus.Confirmed)
        {
            appt.Status = Core.Enums.AppointmentStatus.InProgress;
            await _appointmentRepo.UpdateAsync(appt);
        }

        return Ok(new { Message = "Consultation submitted" });
    }

    [HttpGet("{id}/calendar")]
    public async Task<IActionResult> GetCalendar(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var appt = await _appointmentRepo.GetByIdAsync(id);
        if (appt == null || appt.UserId != userId) return NotFound();

        var cal = new Calendar();
        var evt = new CalendarEvent
        {
            Summary = "Tarot Appointment",
            DtStart = new CalDateTime(appt.StartTime.UtcDateTime),
            DtEnd = new CalDateTime(appt.EndTime.UtcDateTime),
            Description = $"Service {appt.ServiceId} | Price {appt.Price}",
        };
        cal.Events.Add(evt);
        var serializer = new Ical.Net.Serialization.CalendarSerializer();
        var ics = serializer.SerializeToString(cal) ?? string.Empty;
        return File(System.Text.Encoding.UTF8.GetBytes(ics), "text/calendar", "appointment.ics");
    }
}
