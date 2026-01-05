using Tarot.Core.Entities;

namespace Tarot.Core.Interfaces;

public interface IBlogService
{
    Task<IReadOnlyList<BlogPost>> GetAllPostsAsync();
    Task<BlogPost?> GetPostBySlugAsync(string slug);
    Task<BlogPost> CreatePostAsync(BlogPost post);
    Task UpdatePostAsync(BlogPost post);
    Task DeletePostAsync(Guid id);
}
