namespace Tarot.Core.Settings;

public class AppSettings
{
    public bool EnablePayment { get; set; } = true;
    public bool UseOutbox { get; set; } = false;
    public JwtSettings Jwt { get; set; } = new();
    public OutboxSettings Outbox { get; set; } = new();
    public PaymentSettings Payment { get; set; } = new();
    public AdminSettings Admin { get; set; } = new();
}

public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}

public class OutboxSettings
{
    public bool EnableProcessor { get; set; } = false;
    public int IntervalSeconds { get; set; } = 30;
    public int Take { get; set; } = 50;
    public int BaseBackoffSeconds { get; set; } = 30;
    public int MaxBackoffSeconds { get; set; } = 600;
    public int MaxRetries { get; set; } = 5;
}

public class PaymentSettings
{
    public bool MockFail { get; set; } = false;
}

public class AdminSettings
{
    public string DefaultEmail { get; set; } = "admin@example.com";
    public string DefaultPassword { get; set; } = "Passw0rd!";
    public string DefaultName { get; set; } = "Super Admin";
    public List<string> DefaultPermissions { get; set; } =
    [
        "DESIGN_EDIT", "KNOWLEDGE_EDIT", "SCHEDULE_MANAGE", "CONSULTATION_REPLY", 
        "FINANCE_VIEW", "BLOG_MANAGE", "TRASH_MANAGE", "INBOX_MANAGE"
    ];
}
