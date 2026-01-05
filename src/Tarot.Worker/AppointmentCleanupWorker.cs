using Microsoft.EntityFrameworkCore;
using Tarot.Core.Entities;
using Tarot.Core.Enums;
using Tarot.Infrastructure.Data;

namespace Tarot.Worker;

public class AppointmentCleanupWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppointmentCleanupWorker> _logger;

    public AppointmentCleanupWorker(IServiceProvider serviceProvider, ILogger<AppointmentCleanupWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("AppointmentCleanupWorker running at: {time}", DateTimeOffset.Now);

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    // 1. Auto-Cancel: Pending appointments older than 15 minutes
                    var cancelTimeout = DateTimeOffset.UtcNow.AddMinutes(-15);
                    var pendingAppointments = await context.Set<Appointment>()
                        .Include(a => a.User) // Include User if we need to notify (not implemented yet)
                        .Where(a => a.Status == AppointmentStatus.Pending && a.CreatedAt < cancelTimeout)
                        .ToListAsync(stoppingToken);

                    if (pendingAppointments.Any())
                    {
                        foreach (var appt in pendingAppointments)
                        {
                            appt.Status = AppointmentStatus.Cancelled;
                            appt.CancellationReason = "Auto-cancelled due to non-payment timeout (15min)";
                            _logger.LogInformation("Auto-cancelling appointment {Id}", appt.Id);
                        }
                    }

                    // 2. Auto-Complete: Appointments ended > 10 mins ago AND Admin hasn't replied
                    // Condition: EndTime < Now - 10min AND Admin 未回复 (Consultation.RepliedAt is null or Consultation is null)
                    var completeTimeout = DateTimeOffset.UtcNow.AddMinutes(-10);
                    
                    // We need to fetch appointments that are NOT Completed/Cancelled, and time has passed
                    // Assuming 'InProgress' or 'Confirmed' are the states before 'Completed'
                    var potentialCompleteAppointments = await context.Set<Appointment>()
                        .Include(a => a.Consultation)
                        .Include(a => a.User)
                        .Include(a => a.Service)
                        .Where(a => (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.InProgress) 
                                    && a.EndTime < completeTimeout)
                        .ToListAsync(stoppingToken);

                    foreach (var appt in potentialCompleteAppointments)
                    {
                        // Check if Admin has replied
                        bool adminReplied = appt.Consultation?.RepliedAt != null;

                        if (!adminReplied)
                        {
                            // "Condition: EndTime < Now - 10min AND Admin 未回复" -> Auto Complete
                            appt.Status = AppointmentStatus.Completed;
                            appt.AutoCompletedAt = DateTimeOffset.UtcNow;
                            appt.CancellationReason = "Auto-completed by system (Admin no reply timeout)"; // Reusing field or adding note
                            
                            // Grant Loyalty Points
                            if (appt.User != null && appt.Service != null)
                            {
                                int currentCount = appt.User.AppointmentCount + 1;
                                appt.User.AppointmentCount = currentCount;

                                decimal multiplier = 1.0m;
                                if (currentCount >= 6 && currentCount <= 10) multiplier = 1.5m;
                                else if (currentCount >= 11) multiplier = 2.0m;

                                int points = (int)(appt.Service.Price * multiplier); // Assuming 1 point per currency unit * multiplier
                                appt.User.LoyaltyPoints += points;
                                
                                _logger.LogInformation("Granted {Points} points to user {UserId}. Total Orders: {Count}", points, appt.UserId, currentCount);
                            }

                            _logger.LogInformation("Auto-completing appointment {Id}", appt.Id);
                        }
                    }

                    if (context.ChangeTracker.HasChanges())
                    {
                        await context.SaveChangesAsync(stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in AppointmentCleanupWorker");
            }

            // Run every 1 minute
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
