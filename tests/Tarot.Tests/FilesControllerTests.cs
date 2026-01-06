using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Tarot.Api.Controllers;
using Tarot.Core.Interfaces;
using Xunit;

namespace Tarot.Tests;

public class FilesControllerTests
{
    private readonly Mock<IFileStorageService> _mockStorage;
    private readonly FilesController _controller;

    public FilesControllerTests()
    {
        _mockStorage = new Mock<IFileStorageService>();
        _controller = new FilesController(_mockStorage.Object);
    }

    [Fact]
    public async Task Upload_ShouldReturnOk_WhenFileIsValid()
    {
        // Arrange
        var file = new Mock<IFormFile>();
        file.Setup(f => f.FileName).Returns("test.jpg");
        file.Setup(f => f.Length).Returns(1024);
        var ms = new MemoryStream();
        file.Setup(f => f.OpenReadStream()).Returns(ms);

        _mockStorage.Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<long>()))
            .ReturnsAsync("/uploads/test.jpg");

        // Act
        var result = await _controller.Upload(file.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = System.Text.Json.JsonSerializer.Serialize(okResult.Value);
        Assert.Contains("/uploads/test.jpg", json);
    }

    [Fact]
    public async Task Upload_ShouldReturnBadRequest_WhenStorageThrowsArgumentException()
    {
        // Arrange
        var file = new Mock<IFormFile>();
        file.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        
        _mockStorage.Setup(s => s.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string[]>(), It.IsAny<long>()))
            .ThrowsAsync(new ArgumentException("Invalid file type"));

        // Act
        var result = await _controller.Upload(file.Object);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid file type", badRequest.Value);
    }
}