using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;
using Tarot.Core.Enums;

namespace Tarot.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class CardsController(IRepository<Card> cardRepo, IRedisService redis) : ControllerBase
{
    private readonly IRepository<Card> _cardRepo = cardRepo;
    private readonly IRedisService _redis = redis;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? q, [FromQuery] Suit? suit, [FromQuery] ArcanaType? arcana, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var cacheKey = $"cards:{q?.Trim()}:{suit}:{arcana}:{page}:{pageSize}";
        var cached = await _redis.GetAsync<CardListResponse>(cacheKey);
        if (cached != null) return Ok(cached);

        q = (q ?? "").Trim();
        int skip = Math.Max(0, (page - 1) * pageSize);
        System.Linq.Expressions.Expression<Func<Card, bool>> predicate = c =>
            (string.IsNullOrWhiteSpace(q) || (c.Name ?? "").Contains(q)) &&
            (!suit.HasValue || c.Suit == suit.Value) &&
            (!arcana.HasValue || c.ArcanaType == arcana.Value);
        var total = await _cardRepo.CountAsync(predicate);
        var items = await _cardRepo.ListReadOnlyAsync(predicate, skip, pageSize);
        
        var result = new CardListResponse
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = [.. items.Select(c => new CardResponseDto
            {
                Id = c.Id,
                Name = c.Name,
                ImageUrl = c.ImageUrl,
                Suit = c.Suit,
                ArcanaType = c.ArcanaType,
                MeaningUpright = c.MeaningUpright,
                MeaningReversed = c.MeaningReversed,
                Keywords = c.Keywords,
                AdminNotes = c.AdminNotes
            })]
        };

        await _redis.SetAsync(cacheKey, result, TimeSpan.FromMinutes(30));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var card = await _cardRepo.GetByIdAsync(id);
        if (card == null) return NotFound();
        return Ok(card);
    }

    [Authorize(Policy = "KNOWLEDGE_EDIT")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CardCreateDto dto)
    {
        var card = new Card
        {
            Name = dto.Name,
            ImageUrl = dto.ImageUrl,
            Suit = dto.Suit,
            ArcanaType = dto.ArcanaType,
            MeaningUpright = dto.MeaningUpright,
            MeaningReversed = dto.MeaningReversed,
            Keywords = dto.Keywords,
            AdminNotes = dto.AdminNotes,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var created = await _cardRepo.AddAsync(card);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Policy = "KNOWLEDGE_EDIT")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CardUpdateDto dto)
    {
        var card = await _cardRepo.GetByIdAsync(id);
        if (card == null) return NotFound();
        card.Name = dto.Name;
        card.ImageUrl = dto.ImageUrl;
        card.Suit = dto.Suit;
        card.ArcanaType = dto.ArcanaType;
        card.MeaningUpright = dto.MeaningUpright;
        card.MeaningReversed = dto.MeaningReversed;
        card.Keywords = dto.Keywords;
        card.AdminNotes = dto.AdminNotes;
        card.UpdatedAt = DateTimeOffset.UtcNow;
        await _cardRepo.UpdateAsync(card);
        return Ok(card);
    }

    [Authorize(Policy = "KNOWLEDGE_EDIT")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var card = await _cardRepo.GetByIdAsync(id);
        if (card == null) return NoContent();
        await _cardRepo.DeleteAsync(card);
        return NoContent();
    }
}

public class CardCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Suit Suit { get; set; }
    public ArcanaType ArcanaType { get; set; }
    public string? MeaningUpright { get; set; }
    public string? MeaningReversed { get; set; }
    public string? Keywords { get; set; }
    public string? AdminNotes { get; set; }
}

public class CardUpdateDto : CardCreateDto {}

public class CardListResponse
{
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public List<CardResponseDto> Items { get; set; } = [];
}

public class CardResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public Suit Suit { get; set; }
    public ArcanaType ArcanaType { get; set; }
    public string? MeaningUpright { get; set; }
    public string? MeaningReversed { get; set; }
    public string? Keywords { get; set; }
    public string? AdminNotes { get; set; }
}
