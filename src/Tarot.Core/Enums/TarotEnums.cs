namespace Tarot.Core.Enums;

public enum UserRole
{
    Customer = 0,
    Admin = 1,
    SuperAdmin = 2
}

public enum AppointmentStatus
{
    Pending = 0,
    Confirmed = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum PaymentStatus
{
    Unpaid = 0,
    Paid = 1,
    Refunded = 2,
    Skipped = 3
}

public enum Suit
{
    Wands = 0,
    Cups = 1,
    Swords = 2,
    Pentacles = 3,
    MajorArcana = 4
}

public enum ArcanaType
{
    Major = 0,
    Minor = 1
}
