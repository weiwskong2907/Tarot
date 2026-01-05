using Tarot.Core.Entities;
using Tarot.Core.Enums;
using Tarot.Core.Interfaces;

namespace Tarot.Core.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IRepository<Appointment> _appointmentRepo;
    private readonly IRepository<Service> _serviceRepo;

    public AppointmentService(IRepository<Appointment> appointmentRepo, IRepository<Service> serviceRepo)
    {
        _appointmentRepo = appointmentRepo;
        _serviceRepo = serviceRepo;
    }

    public async Task<Appointment> CreateAppointmentAsync(Guid userId, Guid serviceId, DateTime scheduledTime)
    {
        var service = await _serviceRepo.GetByIdAsync(serviceId);
        if (service == null)
            throw new Exception("Service not found");

        var startTime = new DateTimeOffset(scheduledTime, TimeSpan.Zero); // Assuming UTC
        var appointment = new Appointment
        {
            UserId = userId,
            ServiceId = serviceId,
            StartTime = startTime,
            EndTime = startTime.AddMinutes(service.DurationMin),
            Status = AppointmentStatus.Pending,
            Price = service.Price,
            CreatedAt = DateTime.UtcNow
        };

        await _appointmentRepo.AddAsync(appointment);
        return appointment;
    }

    public async Task<IEnumerable<Appointment>> GetUserAppointmentsAsync(Guid userId)
    {
        // simplistic implementation, ideally would use a Specification pattern or custom query
        var all = await _appointmentRepo.ListAllAsync();
        return all.Where(a => a.UserId == userId);
    }

    public async Task<Appointment?> GetAppointmentByIdAsync(Guid id)
    {
        return await _appointmentRepo.GetByIdAsync(id);
    }

    public async Task<bool> CancelAppointmentAsync(Guid id, Guid userId)
    {
        var appointment = await _appointmentRepo.GetByIdAsync(id);
        if (appointment == null || appointment.UserId != userId)
            return false;

        if (appointment.Status == AppointmentStatus.Completed)
            return false;

        appointment.Status = AppointmentStatus.Cancelled;
        await _appointmentRepo.UpdateAsync(appointment);
        return true;
    }
}
