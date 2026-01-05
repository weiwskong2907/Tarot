using Tarot.Core.Entities;
using Tarot.Core.Interfaces;

namespace Tarot.Core.Services;

public class BlogService(IRepository<BlogPost> blogRepo) : IBlogService
{
    private readonly IRepository<BlogPost> _blogRepo = blogRepo;

    public async Task<IReadOnlyList<BlogPost>> GetAllPostsAsync()
    {
        // Simple implementation; in real world, might need pagination
        return await _blogRepo.ListAllAsync();
    }

    public async Task<BlogPost?> GetPostBySlugAsync(string slug)
    {
        var all = await _blogRepo.ListAllAsync();
        return all.FirstOrDefault(p => p.Slug == slug);
    }

    public async Task<BlogPost> CreatePostAsync(BlogPost post)
    {
        // Ensure slug is unique
        var existing = await GetPostBySlugAsync(post.Slug);
        if (existing != null)
            throw new Exception("Slug already exists");
        
        post.CreatedAt = DateTimeOffset.UtcNow;
        return await _blogRepo.AddAsync(post);
    }

    public async Task UpdatePostAsync(BlogPost post)
    {
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await _blogRepo.UpdateAsync(post);
    }

    public async Task DeletePostAsync(Guid id)
    {
        var post = await _blogRepo.GetByIdAsync(id);
        if (post != null)
        {
            await _blogRepo.DeleteAsync(post);
        }
    }
}
