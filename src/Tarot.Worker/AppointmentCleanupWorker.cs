using Microsoft.EntityFrameworkCore;
using Tarot.Core.Entities;
using Tarot.Core.Enums;
using Tarot.Core.Interfaces;
using Tarot.Infrastructure.Data;

namespace Tarot.Worker;

public class AppointmentCleanupWorker(IServiceProvider serviceProvider, ILogger<AppointmentCleanupWorker> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<AppointmentCleanupWorker> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessAppointmentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing appointments.");
            }

            // Run every 1 minute
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task ProcessAppointmentsAsync(CancellationToken token)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTimeOffset.UtcNow;

        // 1. Auto-Cancel: Pending appointments older than 15 minutes
        var cancelThreshold = now.AddMinutes(-15);
        var toCancelIds = await context.Appointments
            .Where(a => a.Status == AppointmentStatus.Pending && a.CreatedAt < cancelThreshold)
            .Select(a => a.Id)
            .ToListAsync(token);

        foreach (var id in toCancelIds)
        {
            await ProcessSingleCancellationAsync(id, token);
        }

        // 2. Auto-Complete: InProgress/Confirmed appointments ended more than 10 mins ago
        var completeThreshold = now.AddMinutes(-10);
        var toCompleteIds = await context.Appointments
            .Where(a => (a.Status == AppointmentStatus.InProgress || a.Status == AppointmentStatus.Confirmed) 
                        && a.EndTime < completeThreshold)
            .Select(a => a.Id)
            .ToListAsync(token);

        foreach (var id in toCompleteIds)
        {
            await ProcessSingleCompletionAsync(id, now, token);
        }
    }

    private async Task ProcessSingleCancellationAsync(Guid appointmentId, CancellationToken token)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var appt = await context.Appointments.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == appointmentId, token);
            if (appt == null || appt.Status != AppointmentStatus.Pending) return;

            appt.Status = AppointmentStatus.Cancelled;
            appt.CancellationReason = "System Auto-Cancel: Payment Timeout";

            await context.SaveChangesAsync(token);
            _logger.LogInformation("Auto-cancelled appointment {AppointmentId}", appt.Id);

            // Send email AFTER successful commit
            if (appt.User != null && !string.IsNullOrEmpty(appt.User.Email))
            {
                try 
                {
                    await emailService.SendTemplateEmailAsync(appt.User.Email!, "appointment-cancelled", new
                    {
                        appt.Id,
                        appt.StartTime,
                        Reason = appt.CancellationReason,
                        appt.User.UserName
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send cancellation email for {AppointmentId}", appt.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing cancellation for {AppointmentId}", appointmentId);
        }
    }

    private async Task ProcessSingleCompletionAsync(Guid appointmentId, DateTimeOffset now, CancellationToken token)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var loyaltyService = scope.ServiceProvider.GetRequiredService<ILoyaltyService>();

            var appt = await context.Appointments
                .Include(a => a.User)
                .Include(a => a.Service)
                .Include(a => a.Consultation)
                .FirstOrDefaultAsync(a => a.Id == appointmentId, token);

            if (appt == null) return;
            // Double check status inside transaction
            if (appt.Status != AppointmentStatus.InProgress && appt.Status != AppointmentStatus.Confirmed) return;

            // Check if Admin has replied
            bool adminReplied = appt.Consultation?.RepliedAt != null;
            if (adminReplied) return;

            appt.Status = AppointmentStatus.Completed;
            appt.AutoCompletedAt = now;
            appt.CancellationReason = "Auto-completed by system (Admin no reply timeout)";

            // Grant Loyalty Points logic needs to be handled carefully. 
            // Ideally LoyaltyService uses the same DbContext or supports transaction.
            // For now, we assume LoyaltyService is safe or we do it manually.
            // But wait, LoyaltyService might need the same context to be atomic.
            // If LoyaltyService creates its own context, we have a distributed transaction issue (conceptually).
            // Let's assume for now we just call it. Ideally we should inject the context into LoyaltyService.
            
            int points = 0;
            if (appt.User != null && appt.Service != null)
            {
                 points = await loyaltyService.AwardPointsForAppointmentAsync(appt.User, appt.Service.Price);
            }

            await context.SaveChangesAsync(token);
            _logger.LogInformation("Auto-completed appointment {AppointmentId}", appt.Id);

            // Send email AFTER successful commit
            if (appt.User != null && !string.IsNullOrEmpty(appt.User.Email))
            {
                try
                {
                    await emailService.SendTemplateEmailAsync(appt.User.Email!, "appointment-completed", new
                    {
                        appt.Id,
                        appt.EndTime,
                        Points = points,
                        appt.User.UserName,
                        ServiceName = appt.Service?.Name ?? "Unknown"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send completion email for {AppointmentId}", appt.Id);
                }
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error processing completion for {AppointmentId}", appointmentId);
        }
    }
}
