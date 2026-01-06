using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Text.Json;
using Tarot.Api.Controllers;
using Tarot.Core.Entities;
using Tarot.Core.Interfaces;
using Tarot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Tarot.Tests;

public class ResourcesControllerTests
{
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly AppDbContext _dbContext;
    private readonly ResourcesController _controller;
    private readonly Mock<IRepository<Service>> _mockServiceRepo;

    public ResourcesControllerTests()
    {
        _mockServiceProvider = new Mock<IServiceProvider>();
        
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new AppDbContext(options);

        _mockServiceRepo = new Mock<IRepository<Service>>();

        // Setup generic resolution
        _mockServiceProvider
            .Setup(x => x.GetService(typeof(IRepository<Service>)))
            .Returns(_mockServiceRepo.Object);

        _controller = new ResourcesController(_mockServiceProvider.Object, _dbContext);

        // Setup User context
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "SuperAdmin")
        ], "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task GetAll_ReturnsOk_WithList()
    {
        // Arrange
        var services = new List<Service> { new() { Name = "Tarot", Price = 100 } };
        _mockServiceRepo.Setup(x => x.ListAllReadOnlyAsync()).ReturnsAsync(services);

        // Act
        var result = await _controller.GetAll("services");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
        Assert.Single(items);
    }

    [Fact]
    public async Task Create_ReturnsCreated_AndLogsAudit()
    {
        // Arrange
        var json = JsonSerializer.SerializeToElement(new { Name = "New Service", Price = 200, DurationMin = 30 });
        _mockServiceRepo.Setup(x => x.AddAsync(It.IsAny<Service>())).ReturnsAsync((Service s) => 
        { 
            s.Id = Guid.NewGuid(); 
            return s; 
        });

        // Act
        var result = await _controller.Create("services", json);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var entity = Assert.IsType<Service>(createdResult.Value);
        Assert.Equal("New Service", entity.Name);

        // Verify Audit Log
        Assert.Single(_dbContext.AuditLogs);
        Assert.Equal("Dynamic_Create_services", _dbContext.AuditLogs.First().Action);
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_InvalidData()
    {
        // Arrange - Missing Required Name
        var json = JsonSerializer.SerializeToElement(new { Price = 200 });

        // Act
        var result = await _controller.Create("services", json);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
