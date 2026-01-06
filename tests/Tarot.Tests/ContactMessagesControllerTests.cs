using Microsoft.AspNetCore.Mvc;
using Moq;
using Tarot.Api.Controllers;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;

namespace Tarot.Tests;

public class ContactMessagesControllerTests
{
    private readonly Mock<IRepository<ContactMessage>> _mockRepo;
    private readonly Mock<IRedisService> _mockRedis;
    private readonly Mock<IEmailService> _mockEmail;
    private readonly ContactMessagesController _controller;

    public ContactMessagesControllerTests()
    {
        _mockRepo = new Mock<IRepository<ContactMessage>>();
        _mockRedis = new Mock<IRedisService>();
        _mockEmail = new Mock<IEmailService>();
        _controller = new ContactMessagesController(_mockRepo.Object, _mockRedis.Object, _mockEmail.Object);
    }

    [Fact]
    public async Task Reply_ShouldUpdateMessageAndSendEmail()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new ContactMessageReplyDto { Reply = "Test Reply" };
        var message = new ContactMessage 
        { 
            Id = id, 
            Name = "John", 
            Email = "john@example.com", 
            Message = "Hello", 
            Status = "Received" 
        };

        _mockRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(message);

        // Act
        var result = await _controller.Reply(id, dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedMessage = Assert.IsType<ContactMessage>(okResult.Value);
        Assert.Equal("Replied", returnedMessage.Status);
        Assert.Equal("Test Reply", returnedMessage.Reply);

        _mockRepo.Verify(r => r.UpdateAsync(It.Is<ContactMessage>(m => m.Status == "Replied" && m.Reply == "Test Reply")), Times.Once);
        _mockEmail.Verify(e => e.SendEmailAsync("john@example.com", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public async Task Reply_ShouldReturnNotFound_WhenMessageDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();
        var dto = new ContactMessageReplyDto { Reply = "Test Reply" };
        _mockRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((ContactMessage?)null);

        // Act
        var result = await _controller.Reply(id, dto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
