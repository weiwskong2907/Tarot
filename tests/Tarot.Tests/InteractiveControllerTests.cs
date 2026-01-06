using Moq;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Tarot.Api.Controllers;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;
using Xunit;

namespace Tarot.Tests;

public class InteractiveControllerTests
{
    private readonly Mock<IRepository<DailyDrawRecord>> _mockDailyRepo;
    private readonly Mock<IRepository<Card>> _mockCardRepo;
    private readonly InteractiveController _controller;

    public InteractiveControllerTests()
    {
        _mockDailyRepo = new Mock<IRepository<DailyDrawRecord>>();
        _mockCardRepo = new Mock<IRepository<Card>>();
        
        _controller = new InteractiveController(_mockDailyRepo.Object, _mockCardRepo.Object);

        // Setup User context
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        ], "mock"));

        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext() { User = user }
        };
    }

    [Fact]
    public async Task SelfReading_ShouldReturnThreeCards_WithPastPresentFuturePositions()
    {
        // Arrange
        var cards = new List<Card>
        {
            new() { Id = Guid.NewGuid(), Name = "Card 1", MeaningUpright = "Up1", MeaningReversed = "Rev1" },
            new() { Id = Guid.NewGuid(), Name = "Card 2", MeaningUpright = "Up2", MeaningReversed = "Rev2" },
            new() { Id = Guid.NewGuid(), Name = "Card 3", MeaningUpright = "Up3", MeaningReversed = "Rev3" },
            new() { Id = Guid.NewGuid(), Name = "Card 4", MeaningUpright = "Up4", MeaningReversed = "Rev4" }
        };
        _mockCardRepo.Setup(repo => repo.ListAllAsync()).ReturnsAsync(cards);

        // Act
        var result = await _controller.SelfReading();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var root = doc.RootElement;

        string message = root.GetProperty("Message").GetString() ?? string.Empty;
        Assert.Equal("Past, Present, Future Reading", message);

        var spread = root.GetProperty("Spread");
        Assert.Equal(3, spread.GetArrayLength());

        var list = spread.EnumerateArray().ToList();
        
        Assert.Equal("Past", list[0].GetProperty("Position").GetString());
        Assert.Equal("Present", list[1].GetProperty("Position").GetString());
        Assert.Equal("Future", list[2].GetProperty("Position").GetString());

        foreach(var item in list)
        {
            Assert.True(item.TryGetProperty("Card", out var cardProp));
            bool isUpright = item.GetProperty("IsUpright").GetBoolean();
            string currentMeaning = cardProp.GetProperty("CurrentMeaning").GetString() ?? string.Empty;
            string uprightMeaning = cardProp.GetProperty("MeaningUpright").GetString() ?? string.Empty;
            string reversedMeaning = cardProp.GetProperty("MeaningReversed").GetString() ?? string.Empty;

            if (isUpright)
            {
                Assert.Equal(uprightMeaning, currentMeaning);
            }
            else
            {
                Assert.Equal(reversedMeaning, currentMeaning);
            }
        }
    }

    [Fact]
    public async Task SelfReading_ShouldReturnBadRequest_WhenNotEnoughCards()
    {
        // Arrange
        var cards = new List<Card>
        {
            new() { Name = "Card 1" },
            new() { Name = "Card 2" }
        };
        _mockCardRepo.Setup(repo => repo.ListAllAsync()).ReturnsAsync(cards);

        // Act
        var result = await _controller.SelfReading();

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Not enough cards available for self-reading.", badRequestResult.Value);
    }
}
