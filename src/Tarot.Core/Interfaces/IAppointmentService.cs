using Tarot.Core.Entities;

namespace Tarot.Core.Interfaces;

public interface IAppointmentService
{
    Task<Appointment> CreateAppointmentAsync(Guid userId, Guid serviceId, DateTime scheduledTime);
    Task<IEnumerable<Appointment>> GetUserAppointmentsAsync(Guid userId);
    Task<Appointment?> GetAppointmentByIdAsync(Guid id);
    Task<bool> CancelAppointmentAsync(Guid id, Guid userId);
}
