using System.ComponentModel.DataAnnotations;

namespace Tarot.Api.Dtos;

public class CreateAppointmentDto
{
    [Required]
    public Guid ServiceId { get; set; }

    [Required]
    public DateTime StartTime { get; set; }
}

public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
