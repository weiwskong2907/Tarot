using Tarot.Core.Entities;

namespace Tarot.Core.Interfaces;

public interface ILoyaltyService
{
    Task<int> AwardPointsForAppointmentAsync(AppUser user, decimal servicePrice);
}
