using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;
using System.Dynamic;

namespace Tarot.Api.Controllers;

[Authorize(Policy = "DESIGN_EDIT")]
[ApiController]
[Route("api/v1/[controller]")]
public class EmailTemplatesController(
    IRepository<EmailTemplate> templateRepo,
    IEmailService emailService) : ControllerBase
{
    private readonly IRepository<EmailTemplate> _templateRepo = templateRepo;
    private readonly IEmailService _emailService = emailService;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _templateRepo.ListAllAsync();
        return Ok(list.Select(t => new
        {
            t.Id,
            t.Slug,
            t.SubjectTpl,
            t.BodyHtml,
            t.CreatedAt,
            t.UpdatedAt
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var t = await _templateRepo.GetByIdAsync(id);
        if (t == null) return NotFound();
        return Ok(t);
    }

    [HttpGet("by-slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var t = await _templateRepo.FirstOrDefaultAsync(x => x.Slug == slug);
        if (t == null) return NotFound();
        return Ok(t);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmailTemplateCreateDto dto)
    {
        var existing = await _templateRepo.FirstOrDefaultAsync(x => x.Slug == dto.Slug);
        if (existing != null) return BadRequest("Slug exists");
        var t = new EmailTemplate
        {
            Slug = dto.Slug,
            SubjectTpl = dto.SubjectTpl,
            BodyHtml = dto.BodyHtml,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var created = await _templateRepo.AddAsync(t);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] EmailTemplateUpdateDto dto)
    {
        var t = await _templateRepo.GetByIdAsync(id);
        if (t == null) return NotFound();
        t.SubjectTpl = dto.SubjectTpl;
        t.BodyHtml = dto.BodyHtml;
        t.UpdatedAt = DateTimeOffset.UtcNow;
        await _templateRepo.UpdateAsync(t);
        return Ok(t);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var t = await _templateRepo.GetByIdAsync(id);
        if (t == null) return NotFound();
        await _templateRepo.DeleteAsync(t);
        return NoContent();
    }

    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] EmailTemplatePreviewDto dto)
    {
        // Convert dictionary to ExpandoObject for property access in Razor
        dynamic model = new ExpandoObject();
        var modelDict = (IDictionary<string, object>)model;
        foreach (var kvp in dto.Model)
        {
            modelDict[kvp.Key] = kvp.Value;
        }

        var result = await _emailService.RenderTemplateAsync(dto.Slug, model);
        if (result.StartsWith("Template '") && result.EndsWith("' not found."))
        {
            return NotFound(result);
        }
        return Ok(new { Html = result });
    }
}

public class EmailTemplatePreviewDto
{
    public string Slug { get; set; } = string.Empty;
    public Dictionary<string, object> Model { get; set; } = [];
}

public class EmailTemplateCreateDto
{
    public string Slug { get; set; } = string.Empty;
    public string SubjectTpl { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
}

public class EmailTemplateUpdateDto
{
    public string SubjectTpl { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
}
