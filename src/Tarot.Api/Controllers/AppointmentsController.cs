using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tarot.Api.Dtos;
using Tarot.Core.Interfaces;

namespace Tarot.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPaymentService _paymentService;

    public AppointmentsController(IAppointmentService appointmentService, IPaymentService paymentService)
    {
        _appointmentService = appointmentService;
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        try
        {
            var appointment = await _appointmentService.CreateAppointmentAsync(userId, dto.ServiceId, dto.StartTime);
            
            // Process Payment (Mock)
            if (appointment.Price > 0)
            {
                var paid = await _paymentService.ProcessPaymentAsync(userId, appointment.Price);
                if (!paid)
                {
                    // In real scenario, we might cancel the appointment or mark as unpaid
                    return BadRequest("Payment failed");
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
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
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

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var result = await _appointmentService.CancelAppointmentAsync(id, userId);

        if (!result)
            return BadRequest("Cannot cancel appointment");

        return Ok(new { Message = "Appointment cancelled" });
    }
}
