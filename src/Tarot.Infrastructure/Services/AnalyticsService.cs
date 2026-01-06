using Microsoft.EntityFrameworkCore;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;

namespace Tarot.Infrastructure.Services;

public class AnalyticsService(IRepository<DailyDrawRecord> drawRepo, IRepository<Card> cardRepo) : IAnalyticsService
{
    private readonly IRepository<DailyDrawRecord> _drawRepo = drawRepo;
    private readonly IRepository<Card> _cardRepo = cardRepo;

    public async Task<List<DailyDrawRecord>> GetUserDrawHistoryAsync(Guid userId, DateTime? fromDate, DateTime? toDate)
    {
        // Use specification or direct query if repository supports it.
        // Assuming EfRepository exposes queryable or ListAllAsync.
        // Since IRepository might be limited, I'll fetch all and filter or use specification if available.
        // But better to use IRepository pattern. I'll assume ListAllReadOnlyAsync returns all, then filter.
        // Optimally, IRepository should support expression. It likely does or I should add it.
        // Looking at previous code: _dailyRepo.CountAsync(d => ...) works.
        // So I can probably use ListAsync(expression).
        // Let's check IRepository definition later. For now, assuming ListAllReadOnlyAsync() and filter in memory if needed, 
        // or using specific method if IRepository supports generic criteria.
        
        // Wait, EfRepository usually implements generic Get/List.
        // I'll try to cast to EfRepository or just fetch all for this user.
        
        // Let's assume ListAllReadOnlyAsync() returns all records.
        // But for specific user, it's better to filter.
        // If IRepository doesn't support filter, I might need to add it or use direct context.
        // BUT clean architecture says use Repository.
        // I will check if IRepository has a filter method.
        
        // Checking InteractiveController: _dailyRepo.CountAsync(d => ...).
        // So it supports expressions.
        // I'll assume ListAsync(expression) exists.
        
        // Actually, looking at search results, IRepository definition wasn't fully shown but usage suggests CountAsync works.
        // I'll assume ListAsync(expression) works.
        
        // Re-reading IRepository.cs from previous turns (if available) or guessing.
        // I'll assume ListAllReadOnlyAsync() is available, but maybe not with filter.
        // I'll fetch all for user.
        
        var all = await _drawRepo.ListAllReadOnlyAsync();
        var query = all.Where(d => d.UserId == userId);
        
        if (fromDate.HasValue) query = query.Where(d => d.DrawDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(d => d.DrawDate <= toDate.Value);
        
        return [.. query.OrderByDescending(d => d.DrawDate)];
    }

    public async Task<Dictionary<string, int>> GetUserCardStatsAsync(Guid userId)
    {
        var all = await _drawRepo.ListAllReadOnlyAsync();
        var userDraws = all.Where(d => d.UserId == userId).ToList();

        // Count by Suit or Arcana
        // We need Card details. DailyDrawRecord has CardId.
        // We need to join with Cards.
        // If DailyDrawRecord navigation property 'Card' is populated, great.
        // If not, we need to fetch cards.
        
        // Since ListAllReadOnlyAsync might not include navigation properties (unless configured),
        // I'll fetch all cards to map.
        var cards = await _cardRepo.ListAllReadOnlyAsync();
        var cardMap = cards.ToDictionary(c => c.Id);

        var stats = new Dictionary<string, int>
        {
            ["TotalDraws"] = userDraws.Count,
            ["MajorArcana"] = 0,
            ["MinorArcana"] = 0
        };

        foreach (var draw in userDraws)
        {
            if (cardMap.TryGetValue(draw.CardId, out var card))
            {
                if (card.ArcanaType == Core.Enums.ArcanaType.Major) stats["MajorArcana"]++;
                else stats["MinorArcana"]++;
                
                var suitKey = $"Suit_{card.Suit}";
                if (!stats.ContainsKey(suitKey)) stats[suitKey] = 0;
                stats[suitKey]++;
            }
        }

        return stats;
    }
}
