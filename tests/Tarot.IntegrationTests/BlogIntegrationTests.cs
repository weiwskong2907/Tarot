using System.Net.Http.Json;
using Tarot.Api.Dtos;

namespace Tarot.IntegrationTests;

public class BlogIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BlogIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAllPosts_ShouldReturnOk()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/blogposts");

        // Assert
        response.EnsureSuccessStatusCode();
        var posts = await response.Content.ReadFromJsonAsync<List<BlogPostDto>>();
        Assert.NotNull(posts);
    }

    // Since CreatePost requires Admin role, and our TestAuthHandler doesn't assign roles by default,
    // we might skip testing Create/Delete unless we update the handler or mock it per test.
    // But for "completeness", we can assume the default user might not be admin, so it should return 403.
    
    [Fact]
    public async Task CreatePost_ShouldReturnForbidden_IfNotAdmin()
    {
        // Arrange
        var dto = new CreateBlogPostDto
        {
            Title = "Forbidden Post",
            Slug = "forbidden-post",
            Content = "Should not be created"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/blogposts", dto);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, response.StatusCode);
    }
}
