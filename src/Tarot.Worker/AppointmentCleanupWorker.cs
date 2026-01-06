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
        // Use AppDbContext directly to support Includes and complex logic
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var loyaltyService = scope.ServiceProvider.GetRequiredService<ILoyaltyService>();
        using var tx = await context.Database.BeginTransactionAsync(token);

        var now = DateTimeOffset.UtcNow;

        // 1. Auto-Cancel: Pending appointments older than 15 minutes
        var cancelThreshold = now.AddMinutes(-15);
        
        var toCancel = await context.Appointments
            .Include(a => a.User)
            .Where(a => a.Status == AppointmentStatus.Pending && a.CreatedAt < cancelThreshold)
            .ToListAsync(token);

        foreach (var appt in toCancel)
        {
            appt.Status = AppointmentStatus.Cancelled;
            appt.CancellationReason = "System Auto-Cancel: Payment Timeout";
            
            _logger.LogInformation("Auto-cancelled appointment {AppointmentId}", appt.Id);
            
            if (appt.User != null)
            {
                if (!string.IsNullOrEmpty(appt.User.Email))
                {
                    await emailService.SendTemplateEmailAsync(appt.User.Email!, "appointment-cancelled", new
                    {
                        appt.Id,
                        appt.StartTime,
                        Reason = appt.CancellationReason,
                        appt.User.UserName
                    });
                }
            }
        }

        // 2. Auto-Complete: InProgress/Confirmed appointments ended more than 10 mins ago AND Admin hasn't replied
        var completeThreshold = now.AddMinutes(-10);
        
        var toComplete = await context.Appointments
            .Include(a => a.User)
            .Include(a => a.Service)
            .Include(a => a.Consultation)
            .Where(a => (a.Status == AppointmentStatus.InProgress || a.Status == AppointmentStatus.Confirmed) 
                        && a.EndTime < completeThreshold)
            .ToListAsync(token);

        foreach (var appt in toComplete)
        {
            // Check if Admin has replied
            bool adminReplied = appt.Consultation?.RepliedAt != null;

            if (!adminReplied)
            {
                appt.Status = AppointmentStatus.Completed;
                appt.AutoCompletedAt = now;
                appt.CancellationReason = "Auto-completed by system (Admin no reply timeout)";

                // Grant Loyalty Points
                if (appt.User != null && appt.Service != null)
                {
                    int points = await loyaltyService.AwardPointsForAppointmentAsync(appt.User, appt.Service.Price);
                    
                    _logger.LogInformation("Granted {Points} points to user {UserId}", points, appt.UserId);

                    if (!string.IsNullOrEmpty(appt.User.Email))
                    {
                        await emailService.SendTemplateEmailAsync(appt.User.Email!, "appointment-completed", new
                        {
                            appt.Id,
                            appt.EndTime,
                            Points = points,
                            appt.User.UserName,
                            ServiceName = appt.Service.Name
                        });
                    }
                }
                
                _logger.LogInformation("Auto-completed appointment {AppointmentId}", appt.Id);
            }
        }
        
        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(token);
            await tx.CommitAsync(token);
        }
        else
        {
            await tx.RollbackAsync(token);
        }
    }
}
