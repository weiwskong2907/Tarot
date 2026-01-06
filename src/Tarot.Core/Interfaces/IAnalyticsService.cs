using Tarot.Core.Entities;

namespace Tarot.Core.Interfaces;

public interface IAnalyticsService
{
    Task<List<DailyDrawRecord>> GetUserDrawHistoryAsync(Guid userId, DateTime? fromDate, DateTime? toDate);
    Task<Dictionary<string, int>> GetUserCardStatsAsync(Guid userId);
}
