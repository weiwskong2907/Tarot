using System.ComponentModel.DataAnnotations;
using Tarot.Core.Enums;

namespace Tarot.Core.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public class Service : BaseEntity
{
    [Required]
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int DurationMin { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Appointment : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid ServiceId { get; set; }
    public Service? Service { get; set; }

    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }

    public decimal Price { get; set; }

    public AppointmentStatus Status { get; set; }
    public PaymentStatus PaymentStatus { get; set; }

    public string? MeetingLink { get; set; }
    public string? CancellationReason { get; set; }
    public DateTimeOffset? AutoCompletedAt { get; set; }
    public int RescheduleCount { get; set; }

    public Consultation? Consultation { get; set; }
}

public class Consultation : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    public string? Question { get; set; }
    public string? UserImages { get; set; } // JSONB

    public string? Reply { get; set; }
    public string? ReplyImages { get; set; } // JSONB
    public DateTimeOffset? RepliedAt { get; set; }
}

public class Card : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? NameCn { get; set; } // Chinese Name
    public string? ImageUrl { get; set; }
    public Suit Suit { get; set; }
    public ArcanaType ArcanaType { get; set; }
    public string? MeaningUpright { get; set; }
    public string? MeaningUprightCn { get; set; } // Chinese Meaning Upright
    public string? MeaningReversed { get; set; }
    public string? MeaningReversedCn { get; set; } // Chinese Meaning Reversed
    public string? Keywords { get; set; } // JSONB
    public string? AdminNotes { get; set; }
}

public class DailyDrawRecord : BaseEntity
{
    public Guid UserId { get; set; }
    public AppUser? User { get; set; }

    public Guid CardId { get; set; }
    public Card? Card { get; set; }

    public DateTime DrawDate { get; set; }
    public string? Notes { get; set; }
}

public class BlogPost : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? SeoMeta { get; set; } // JSONB
}
