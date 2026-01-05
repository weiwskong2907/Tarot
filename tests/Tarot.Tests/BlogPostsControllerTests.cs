using Microsoft.AspNetCore.Mvc;
using Moq;
using Tarot.Api.Controllers;
using Tarot.Api.Dtos;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;

namespace Tarot.Tests;

public class BlogPostsControllerTests
{
    private readonly Mock<IBlogService> _mockBlogService;
    private readonly BlogPostsController _controller;

    public BlogPostsControllerTests()
    {
        _mockBlogService = new Mock<IBlogService>();
        _controller = new BlogPostsController(_mockBlogService.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnList()
    {
        // Arrange
        var posts = new List<BlogPost>
        {
            new BlogPost { Title = "T1", Slug = "s1" },
            new BlogPost { Title = "T2", Slug = "s2" }
        };

        _mockBlogService.Setup(s => s.GetAllPostsAsync())
            .ReturnsAsync(posts);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dtos = Assert.IsAssignableFrom<IEnumerable<BlogPostDto>>(okResult.Value);
        Assert.Equal(2, dtos.Count());
    }

    [Fact]
    public async Task GetBySlug_ShouldReturnPost_WhenFound()
    {
        // Arrange
        var slug = "test-slug";
        var post = new BlogPost { Title = "T1", Slug = slug };

        _mockBlogService.Setup(s => s.GetPostBySlugAsync(slug))
            .ReturnsAsync(post);

        // Act
        var result = await _controller.GetBySlug(slug);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<BlogPostDto>(okResult.Value);
        Assert.Equal(slug, dto.Slug);
    }

    [Fact]
    public async Task Create_ShouldReturnCreated()
    {
        // Arrange
        var dto = new CreateBlogPostDto { Title = "T", Slug = "s", Content = "C" };
        var created = new BlogPost { Title = "T", Slug = "s", Content = "C" };

        _mockBlogService.Setup(s => s.CreatePostAsync(It.IsAny<BlogPost>()))
            .ReturnsAsync(created);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(BlogPostsController.GetBySlug), createdResult.ActionName);
        Assert.Equal(created.Slug, createdResult.RouteValues?["slug"]);
    }
}
