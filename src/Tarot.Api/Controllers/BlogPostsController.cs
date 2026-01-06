using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tarot.Api.Dtos;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;

namespace Tarot.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class BlogPostsController(IBlogService blogService) : ControllerBase
{
    private readonly IBlogService _blogService = blogService;

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok((await _blogService.GetAllPostsAsync()).Select(p => new BlogPostDto
        {
            Id = p.Id,
            Title = p.Title,
            Slug = p.Slug,
            Content = p.Content,
            CreatedAt = p.CreatedAt
        }));

    [HttpGet("{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var post = await _blogService.GetPostBySlugAsync(slug);
        if (post == null) return NotFound();

        return Ok(new BlogPostDto
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Content = post.Content,
            CreatedAt = post.CreatedAt
        });
    }

    [Authorize(Policy = "BLOG_MANAGE")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBlogPostDto dto)
    {
        try
        {
            var post = new BlogPost
            {
                Title = dto.Title,
                Slug = dto.Slug,
                Content = dto.Content,
                SeoMeta = dto.SeoMeta
            };
            var created = await _blogService.CreatePostAsync(post);
            return CreatedAtAction(nameof(GetBySlug), new { slug = created.Slug }, created);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [Authorize(Policy = "BLOG_MANAGE")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateBlogPostDto dto)
    {
        var existing = await _blogService.GetPostBySlugAsync(dto.Slug);
        if (existing == null || existing.Id != id)
        {
            return NotFound();
        }
        existing.Title = dto.Title;
        existing.Content = dto.Content;
        existing.SeoMeta = dto.SeoMeta;
        await _blogService.UpdatePostAsync(existing);
        return Ok(existing);
    }

    [Authorize(Policy = "BLOG_MANAGE")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _blogService.DeletePostAsync(id);
        return NoContent();
    }
}
