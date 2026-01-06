using System.ComponentModel.DataAnnotations;

namespace Tarot.Core.Entities;

public class BlockedSlot : BaseEntity
{
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string? Reason { get; set; } // e.g., "Holiday", "Personal Leave"
    
    // Admin who created this block
    public Guid CreatedBy { get; set; }
}
