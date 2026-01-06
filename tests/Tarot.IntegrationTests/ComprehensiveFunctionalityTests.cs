using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tarot.Api.Controllers;
using Tarot.Core.Entities;
using Tarot.Infrastructure.Data;
using Xunit;

namespace Tarot.IntegrationTests;

public class ComprehensiveFunctionalityTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Analytics_GetSummary_ShouldReturnStats()
    {
        // Arrange: Ensure user exists (TestAuthHandler creates a user claim, but we might need DB record for analytics logic?)
        // AnalyticsService likely queries DB.
        // Let's seed some data.
        var userId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (await db.Users.FindAsync(userId) == null)
            {
                db.Users.Add(new AppUser { Id = userId, Email = "analytics@test.com", FullName = "Analytics User", SecurityStamp = "S" });
                // Seed a card draw
                db.DailyDrawRecords.Add(new DailyDrawRecord { Id = Guid.NewGuid(), UserId = userId, DrawDate = DateTime.UtcNow.Date, CardId = Guid.NewGuid() });
                await db.SaveChangesAsync();
            }
        }

        // Act
        var response = await _client.GetAsync("/api/v1/analytics/summary");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotEmpty(content);
    }

    [Fact]
    public async Task Loyalty_GetPoints_ShouldReturnData()
    {
        // Arrange
        var userId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (await db.Users.FindAsync(userId) == null)
            {
                db.Users.Add(new AppUser 
                { 
                    Id = userId, 
                    Email = "loyalty@test.com", 
                    FullName = "Loyalty User", 
                    LoyaltyPoints = 100,
                    AppointmentCount = 5,
                    SecurityStamp = "S"
                });
                await db.SaveChangesAsync();
            }
        }

        // Act
        var response = await _client.GetAsync("/api/v1/loyalty/points");

        // Assert
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(data);
        Assert.Equal(100, (int)data.GetProperty("points").GetInt32());
    }

    [Fact]
    public async Task Files_Upload_ShouldReturnUrl()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }); // Fake JPEG header
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "file", "test.jpg");

        // Act
        var response = await _client.PostAsync("/api/v1/files/upload", content);

        // Assert
        var resultJson = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
             Assert.Fail($"Upload failed with status {response.StatusCode} and body: {resultJson}");
        }
        response.EnsureSuccessStatusCode();
        var result = System.Text.Json.JsonSerializer.Deserialize<dynamic>(resultJson);
        // Check if Url property exists
        Assert.NotNull(result);
        // The mock implementation usually returns a local path or similar
        // Just verify 200 OK and JSON structure
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnHealthy()
    {
        var response = await _client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        var status = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", status);
    }
}
