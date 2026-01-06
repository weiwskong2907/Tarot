using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;
using System.Text.Json;

using Microsoft.AspNetCore.RateLimiting;

using Tarot.Api.Dtos;

namespace Tarot.Api.Controllers;

[EnableRateLimiting("strict")]
[Authorize]
[ApiController]
[Route("api/v1")]
public class InteractiveController(IRepository<DailyDrawRecord> dailyRepo, IRepository<Card> cardRepo, IAiService aiService) : ControllerBase
{
    private readonly IRepository<DailyDrawRecord> _dailyRepo = dailyRepo;
    private readonly IRepository<Card> _cardRepo = cardRepo;
    private readonly IAiService _aiService = aiService;

    [HttpPost("interactive/ai-interpret")]
    public async Task<IActionResult> AiInterpret([FromBody] AiInterpretationRequestDto dto)
    {
        if (dto.CardIds.Count == 0) return BadRequest("At least one card must be selected.");

        // Fetch card names for interpretation
        // We can do this by ID.
        var cardNames = new List<string>();
        foreach (var id in dto.CardIds)
        {
            var card = await _cardRepo.GetByIdAsync(id);
            if (card != null)
            {
                cardNames.Add(card.Name);
            }
        }

        if (cardNames.Count == 0) return BadRequest("Invalid card IDs provided.");

        var interpretation = await _aiService.InterpretTarotSpreadAsync(dto.SpreadType, cardNames, dto.Question);
        return Ok(new { Interpretation = interpretation });
    }

    [HttpPost("daily-draw")]
    public async Task<IActionResult> DailyDraw()
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        // Check if already drawn today
        var existingCount = await _dailyRepo.CountAsync(d => d.UserId == userId && d.DrawDate >= today && d.DrawDate < tomorrow);

        if (existingCount > 0)
        {
            return BadRequest("You have already drawn a card today.");
        }

        // Fetch cards and randomly select one
        var cards = await _cardRepo.ListAllReadOnlyAsync();
        if (cards.Count == 0)
        {
            return BadRequest("No cards available in the deck.");
        }
        var selected = cards[Random.Shared.Next(cards.Count)];
        
        var record = new DailyDrawRecord
        {
            UserId = userId,
            CardId = selected.Id,
            DrawDate = DateTime.UtcNow,
            Notes = "Daily Draw"
        };

        await _dailyRepo.AddAsync(record);

        return Ok(new 
        { 
            Message = "Card drawn", 
            CardId = selected.Id,
            Card = new { selected.Name, selected.NameCn, selected.Suit, selected.ArcanaType, selected.ImageUrl, selected.MeaningUpright, selected.MeaningUprightCn, selected.MeaningReversed, selected.MeaningReversedCn }
        });
    }

    [HttpPost("self-reading")]
    public async Task<IActionResult> SelfReading()
    {
        var cards = await _cardRepo.ListAllReadOnlyAsync();
        if (cards.Count < 3)
        {
            return BadRequest("Not enough cards available for self-reading.");
        }

        // Pick 3 distinct cards
        var indices = new HashSet<int>();
        while (indices.Count < 3)
        {
            indices.Add(Random.Shared.Next(cards.Count));
        }

        var selectedCards = indices.Select(i => cards[i]).ToList();
        var positions = new[] { "Past", "Present", "Future" };
        var spread = new List<object>();

        for (int i = 0; i < 3; i++)
        {
            var card = selectedCards[i];
            // Randomly determine orientation (50% chance)
            bool isUpright = Random.Shared.Next(2) == 0;

            spread.Add(new 
            {
                Position = positions[i],
                IsUpright = isUpright,
                Card = new 
                { 
                    card.Id, 
                    card.Name, 
                    card.Suit, 
                    card.ArcanaType, 
                    card.ImageUrl, 
                    // Return both meanings but highlight the relevant one
                    card.MeaningUpright, 
                    card.MeaningReversed,
                    CurrentMeaning = isUpright ? card.MeaningUpright : card.MeaningReversed
                }
            });
        }

        return Ok(new { Message = "Past, Present, Future Reading", Spread = spread });
    }
}
