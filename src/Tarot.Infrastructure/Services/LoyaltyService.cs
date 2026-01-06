using Tarot.Core.Entities;
using Tarot.Core.Interfaces;

namespace Tarot.Infrastructure.Services;

public class LoyaltyService : ILoyaltyService
{
    public Task<int> AwardPointsForAppointmentAsync(AppUser user, decimal servicePrice)
    {
        int currentCount = user.AppointmentCount + 1;
        user.AppointmentCount = currentCount;

        decimal multiplier = 1.0m;
        if (currentCount >= 6 && currentCount <= 10) multiplier = 1.5m;
        else if (currentCount >= 11) multiplier = 2.0m;

        int points = (int)(servicePrice * multiplier);
        user.LoyaltyPoints += points;

        return Task.FromResult(points);
    }

    public string GetLoyaltyLevel(int appointmentCount)
    {
        if (appointmentCount >= 11) return "Gold";
        if (appointmentCount >= 6) return "Silver";
        return "Bronze";
    }
}
