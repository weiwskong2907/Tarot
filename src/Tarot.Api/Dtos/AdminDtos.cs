namespace Tarot.Api.Dtos;

public class CancelRequest
{
    public string? Reason { get; set; }
}

public class ReplyRequest
{
    public string Message { get; set; } = string.Empty;
}

public class BlockSlotRequest
{
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public string? Reason { get; set; }
}

public class CreateStaffRequest
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<string>? Permissions { get; set; }
}

public class UpdatePermissionsRequest
{
    public List<string>? Permissions { get; set; }
}

public class RestoreRequest
{
    public string Entity { get; set; } = string.Empty;
    public Guid Id { get; set; }
}
