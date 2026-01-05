using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tarot.Core.Entities;

public class AppUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public int LoyaltyPoints { get; set; }
    public int AppointmentCount { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Permissions { get; set; } // List<string> serialized

    [Column(TypeName = "jsonb")]
    public string? Tags { get; set; } // List<string> serialized
    
    // Navigation properties
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<DailyDrawRecord> DailyDrawRecords { get; set; } = new List<DailyDrawRecord>();
}
