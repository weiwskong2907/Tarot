using Moq;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;
using Tarot.Core.Services;

namespace Tarot.Tests;

public class BlogServiceTests
{
    private readonly Mock<IRepository<BlogPost>> _mockRepo;
    private readonly BlogService _service;

    public BlogServiceTests()
    {
        _mockRepo = new Mock<IRepository<BlogPost>>();
        _service = new BlogService(_mockRepo.Object);
    }

    [Fact]
    public async Task CreatePostAsync_ShouldCreatePost_WhenSlugIsUnique()
    {
        // Arrange
        var post = new BlogPost { Title = "Test", Slug = "test-post", Content = "Content" };
        
        _mockRepo.Setup(r => r.ListAllAsync())
            .ReturnsAsync(new List<BlogPost>()); // No existing posts

        _mockRepo.Setup(r => r.AddAsync(post))
            .ReturnsAsync(post);

        // Act
        var result = await _service.CreatePostAsync(post);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(post.Slug, result.Slug);
        _mockRepo.Verify(r => r.AddAsync(post), Times.Once);
    }

    [Fact]
    public async Task CreatePostAsync_ShouldThrow_WhenSlugExists()
    {
        // Arrange
        var post = new BlogPost { Title = "Test", Slug = "existing-slug", Content = "Content" };
        var existing = new BlogPost { Slug = "existing-slug" };

        _mockRepo.Setup(r => r.ListAllAsync())
            .ReturnsAsync(new List<BlogPost> { existing });

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.CreatePostAsync(post));
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<BlogPost>()), Times.Never);
    }

    [Fact]
    public async Task GetPostBySlugAsync_ShouldReturnPost_WhenExists()
    {
        // Arrange
        var slug = "test-slug";
        var post = new BlogPost { Slug = slug };
        
        _mockRepo.Setup(r => r.ListAllAsync())
            .ReturnsAsync(new List<BlogPost> { post });

        // Act
        var result = await _service.GetPostBySlugAsync(slug);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(slug, result.Slug);
    }
}
