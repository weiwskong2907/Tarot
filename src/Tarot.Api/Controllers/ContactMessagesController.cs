using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;

namespace Tarot.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ContactMessagesController(IRepository<ContactMessage> messageRepo, IRedisService redisService) : ControllerBase
{
    private readonly IRepository<ContactMessage> _messageRepo = messageRepo;
    private readonly IRedisService _redis = redisService;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ContactMessageCreateDto dto)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"rl:contact:{ip}";
        var state = await _redis.GetAsync<RateLimitState>(key) ?? new RateLimitState { Count = 0 };
        if (state.Count >= 5)
            return StatusCode(429, "Too many requests");
        state.Count++;
        await _redis.SetAsync(key, state, TimeSpan.FromMinutes(10));

        var m = new ContactMessage
        {
            Name = dto.Name,
            Email = dto.Email,
            Message = dto.Message,
            Status = "Received",
            CreatedAt = DateTimeOffset.UtcNow
        };
        var created = await _messageRepo.AddAsync(m);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [Authorize(Policy = "INBOX_MANAGE")]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _messageRepo.ListAllAsync();
        return Ok(list.Select(m => new
        {
            m.Id,
            m.Name,
            m.Email,
            m.Message,
            m.Reply,
            m.Status,
            m.CreatedAt,
            m.UpdatedAt
        }));
    }

    [Authorize(Policy = "INBOX_MANAGE")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var m = await _messageRepo.GetByIdAsync(id);
        if (m == null) return NotFound();
        return Ok(m);
    }

    [Authorize(Policy = "INBOX_MANAGE")]
    [HttpPut("{id:guid}/reply")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ContactMessageReplyDto dto)
    {
        var m = await _messageRepo.GetByIdAsync(id);
        if (m == null) return NotFound();
        m.Reply = dto.Reply;
        m.Status = "Replied";
        m.UpdatedAt = DateTimeOffset.UtcNow;
        await _messageRepo.UpdateAsync(m);
        return Ok(m);
    }

    [Authorize(Policy = "INBOX_MANAGE")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var m = await _messageRepo.GetByIdAsync(id);
        if (m == null) return NotFound();
        await _messageRepo.DeleteAsync(m);
        return NoContent();
    }
}

public class ContactMessageCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ContactMessageReplyDto
{
    public string Reply { get; set; } = string.Empty;
}

public class RateLimitState
{
    public int Count { get; set; }
}
