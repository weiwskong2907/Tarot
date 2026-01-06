using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Tarot.Infrastructure.Data;
using Tarot.Core.Interfaces;

namespace Tarot.Api;

public class OutboxProcessorHostedService(IServiceProvider services, ILogger<OutboxProcessorHostedService> logger, IConfiguration config) : BackgroundService
{
    private readonly IServiceProvider _services = services;
    private readonly ILogger<OutboxProcessorHostedService> _logger = logger;
    private readonly IConfiguration _config = config;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = (Environment.GetEnvironmentVariable("ENABLE_OUTBOX_PROCESSOR") ?? _config["EnableOutboxProcessor"] ?? "false")
            .Equals("true", StringComparison.OrdinalIgnoreCase);
        if (!enabled)
        {
            _logger.LogInformation("Outbox processor disabled");
            return;
        }

        var intervalSecStr = Environment.GetEnvironmentVariable("OUTBOX_INTERVAL_SECONDS") ?? _config["Outbox:IntervalSeconds"] ?? "30";
        var intervalSec = int.TryParse(intervalSecStr, out var s) ? Math.Max(5, s) : 30;
        var takeStr = Environment.GetEnvironmentVariable("OUTBOX_TAKE") ?? _config["Outbox:Take"] ?? "50";
        var take = int.TryParse(takeStr, out var t) ? Math.Max(1, t) : 50;
        var baseBackoffStr = Environment.GetEnvironmentVariable("OUTBOX_BASE_BACKOFF_SECONDS") ?? _config["Outbox:BaseBackoffSeconds"] ?? "30";
        var maxBackoffStr = Environment.GetEnvironmentVariable("OUTBOX_MAX_BACKOFF_SECONDS") ?? _config["Outbox:MaxBackoffSeconds"] ?? "600";
        var maxRetriesStr = Environment.GetEnvironmentVariable("OUTBOX_MAX_RETRIES") ?? _config["Outbox:MaxRetries"] ?? "5";
        var baseBackoff = int.TryParse(baseBackoffStr, out var bb) ? Math.Max(5, bb) : 30;
        var maxBackoff = int.TryParse(maxBackoffStr, out var mb) ? Math.Max(baseBackoff, mb) : 600;
        var maxRetries = int.TryParse(maxRetriesStr, out var mr) ? Math.Max(1, mr) : 5;

        _logger.LogInformation("Outbox processor enabled: interval={interval}s, batch={take}", intervalSec, take);

        var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSec));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                var now = DateTimeOffset.UtcNow;
                var pendings = await db.OutboxMessages.Where(x => x.Status == "Pending" && (x.NextAttemptAt == null || x.NextAttemptAt <= now))
                    .OrderBy(x => x.CreatedAt).Take(take).ToListAsync(stoppingToken);
                if (pendings.Count == 0)
                {
                    continue;
                }
                int sent = 0, failed = 0;
                foreach (var m in pendings)
                {
                    try
                    {
                        var doc = JsonDocument.Parse(m.Payload);
                        var root = doc.RootElement;
                        if (m.Type == "email")
                        {
                            var to = root.GetProperty("To").GetString()!;
                            var subject = root.GetProperty("Subject").GetString()!;
                            var body = root.GetProperty("Body").GetString()!;
                            var isHtml = root.TryGetProperty("IsHtml", out var ih) && ih.ValueKind == JsonValueKind.True;
                            await emailService.SendEmailAsync(to, subject, body, isHtml);
                        }
                        else if (m.Type == "email.template")
                        {
                            var to = root.GetProperty("To").GetString()!;
                            var slug = root.GetProperty("TemplateSlug").GetString()!;
                            object model = new { };
                            if (root.TryGetProperty("Model", out var me))
                            {
                                model = ToModel(me) ?? new { };
                            }
                            await emailService.SendTemplateEmailAsync(to, slug, model);
                        }
                        m.Status = "Sent";
                        m.SentAt = DateTimeOffset.UtcNow;
                        db.OutboxMessages.Update(m);
                        await db.SaveChangesAsync(stoppingToken);
                        sent++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Outbox message failed: {MessageId}", m.Id);
                        if (m.RetryCount >= maxRetries)
                        {
                            m.Status = "Failed";
                        }
                        else
                        {
                            m.RetryCount += 1;
                            var backoff = Math.Min(maxBackoff, (int)(baseBackoff * Math.Pow(2, m.RetryCount - 1)));
                            m.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(backoff);
                            m.Status = "Pending";
                        }
                        db.OutboxMessages.Update(m);
                        await db.SaveChangesAsync(stoppingToken);
                        failed++;
                    }
                }
                _logger.LogInformation("Outbox processed: total={total}, sent={sent}, failed={failed}", pendings.Count, sent, failed);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Outbox processor stopping");
        }
    }

    private static object? ToModel(JsonElement el)
    {
        switch (el.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var p in el.EnumerateObject())
                {
                    dict[p.Name] = ToModel(p.Value);
                }
                IDictionary<string, object?> exp = new System.Dynamic.ExpandoObject();
                foreach (var kv in dict) exp[kv.Key] = kv.Value;
                return exp;
            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var i in el.EnumerateArray()) list.Add(ToModel(i));
                return list;
            case JsonValueKind.String:
                return el.GetString();
            case JsonValueKind.Number:
                if (el.TryGetInt64(out var l)) return l;
                if (el.TryGetDouble(out var d)) return d;
                return el.GetRawText();
            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
            default:
                return null;
        }
    }
}
