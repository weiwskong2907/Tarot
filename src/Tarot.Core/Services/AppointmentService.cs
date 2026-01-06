using Tarot.Core.Entities;
using Tarot.Core.Enums;
using Tarot.Core.Interfaces;

namespace Tarot.Core.Services;

public class AppointmentService(
    IRepository<Appointment> appointmentRepo, 
    IRepository<Service> serviceRepo,
    IRepository<AppUser> userRepo,
    IRepository<BlockedSlot> blockedSlotRepo,
    IRedisService redisService,
    IEmailService emailService) : IAppointmentService
{
    private readonly IRepository<Appointment> _appointmentRepo = appointmentRepo;
    private readonly IRepository<Service> _serviceRepo = serviceRepo;
    private readonly IRepository<AppUser> _userRepo = userRepo;
    private readonly IRepository<BlockedSlot> _blockedSlotRepo = blockedSlotRepo;
    private readonly IRedisService _redisService = redisService;
    private readonly IEmailService _emailService = emailService;

    public async Task<Appointment> CreateAppointmentAsync(Guid userId, Guid serviceId, DateTime scheduledTime)
    {
        var service = await _serviceRepo.GetByIdAsync(serviceId) ?? throw new Exception("Service not found");

        var startTime = new DateTimeOffset(scheduledTime, TimeSpan.Zero);
        var endTime = startTime.AddMinutes(service.DurationMin);

        // 1. Redis Distributed Lock to prevent double booking
        // Use day-level lock to prevent overlapping time slots race conditions
        var lockKey = $"lock:appointment:day:{scheduledTime:yyyyMMdd}";
        var acquired = await _redisService.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(10));
        if (!acquired)
        {
            throw new Exception("System busy, please try again.");
        }

        try
        {
            // 2. Check DB availability
            // Check Blocked Slots
            var blockedCount = await _blockedSlotRepo.CountAsync(b => 
                b.StartTime < endTime && b.EndTime > startTime);
            
            if (blockedCount > 0)
                throw new Exception("Time slot is blocked by admin.");

            // Ensure no overlap: (Start < NewEnd) and (End > NewStart)
            var existingCount = await _appointmentRepo.CountAsync(a => a.Status != AppointmentStatus.Cancelled && 
                      a.StartTime < endTime && 
                      a.EndTime > startTime);

            if (existingCount > 0)
            {
                throw new Exception("Time slot is not available.");
            }

            // 3. Create Appointment
            var appointment = new Appointment
            {
                UserId = userId,
                ServiceId = serviceId,
                StartTime = startTime,
                EndTime = endTime,
                Status = AppointmentStatus.Pending,
                Price = service.Price,
                CreatedAt = DateTime.UtcNow,
                PaymentStatus = PaymentStatus.Unpaid
            };

            await _appointmentRepo.AddAsync(appointment);

            // 4. Send Notification (Async fire and forget or queued)
            // In a real app, this should probably be a background job
            // await _emailService.SendTemplateEmailAsync(userEmail, "appointment-created", appointment);

            return appointment;
        }
        finally
        {
            await _redisService.ReleaseLockAsync(lockKey);
        }
    }

    public Task<IReadOnlyList<Appointment>> GetUserAppointmentsAsync(Guid userId) =>
        _appointmentRepo.ListAsync(a => a.UserId == userId);

    public Task<Appointment?> GetAppointmentByIdAsync(Guid id) =>
        _appointmentRepo.GetByIdAsync(id);

    public async Task<bool> CancelAppointmentAsync(Guid id, Guid userId, string? reason = null)
    {
        var appointment = await _appointmentRepo.GetByIdAsync(id);
        if (appointment is null || appointment.UserId != userId)
            return false;

        if (appointment.Status is AppointmentStatus.Completed)
            return false;

        appointment.Status = AppointmentStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            appointment.CancellationReason = reason;
        }
        await _appointmentRepo.UpdateAsync(appointment);
        
        // Return inventory/lock if necessary (implicit by Status change)
        
        return true;
    }

    public async Task<Appointment> RescheduleAppointmentAsync(Guid id, Guid userId, DateTime newTime)
    {
        var appt = await _appointmentRepo.GetByIdAsync(id) ?? throw new Exception("Appointment not found");
        if (appt.UserId != userId) throw new Exception("Unauthorized");
        
        if (appt.Status == AppointmentStatus.Completed || appt.Status == AppointmentStatus.Cancelled)
            throw new Exception("Cannot reschedule completed or cancelled appointment");

        if (appt.RescheduleCount >= 2)
            throw new Exception("Reschedule limit reached");

        var service = await _serviceRepo.GetByIdAsync(appt.ServiceId) ?? throw new Exception("Service data corrupt");

        var startTime = new DateTimeOffset(newTime, TimeSpan.Zero);
        var endTime = startTime.AddMinutes(service.DurationMin);

        // Lock
        var lockKey = $"lock:appointment:day:{newTime:yyyyMMdd}";
        var acquired = await _redisService.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(10));
        if (!acquired) throw new Exception("System busy, please try again.");

        try
        {
            // Check Blocked Slots
            var blockedCount = await _blockedSlotRepo.CountAsync(b => 
                b.StartTime < endTime && b.EndTime > startTime);
            
            if (blockedCount > 0) throw new Exception("Time slot is blocked by admin.");

            // Check Availability (exclude self)
            var count = await _appointmentRepo.CountAsync(a => a.Id != id && 
                a.Status != AppointmentStatus.Cancelled &&
                a.StartTime < endTime && a.EndTime > startTime);

            if (count > 0) throw new Exception("Time slot is not available.");

            appt.StartTime = startTime;
            appt.EndTime = endTime;
            appt.RescheduleCount++;
            await _appointmentRepo.UpdateAsync(appt);
            
            return appt;
        }
        finally
        {
            await _redisService.ReleaseLockAsync(lockKey);
        }
    }
}
