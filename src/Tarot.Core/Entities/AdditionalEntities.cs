using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tarot.Core.Entities;

public class SiteSetting : BaseEntity
{
    [Required]
    public string Key { get; set; } = null!;

    [Column(TypeName = "jsonb")]
    public string? Value { get; set; } // JSONB
}

public class EmailTemplate : BaseEntity
{
    [Required]
    public string Slug { get; set; } = null!; // Unique

    public string SubjectTpl { get; set; } = null!;
    public string BodyHtml { get; set; } = null!;
}

public class ContactMessage : BaseEntity
{
    [Required]
    public string Name { get; set; } = null!;

    [Required]
    public string Email { get; set; } = null!;

    [Required]
    public string Message { get; set; } = null!;

    public string? Reply { get; set; }
    
    public string Status { get; set; } = "New"; // New, Replied, Archived
}

public class AuditLog : BaseEntity
{
    public Guid ActorId { get; set; }
    public string Action { get; set; } = null!;
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}
