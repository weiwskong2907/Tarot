using Moq;
using Tarot.Core.Entities;
using Tarot.Core.Enums;
using Tarot.Core.Interfaces;
using Tarot.Core.Services;
using Xunit;

namespace Tarot.Tests;

public class AppointmentServiceTests
{
    private readonly Mock<IRepository<Appointment>> _mockAppointmentRepo;
    private readonly Mock<IRepository<Service>> _mockServiceRepo;
    private readonly AppointmentService _service;

    public AppointmentServiceTests()
    {
        _mockAppointmentRepo = new Mock<IRepository<Appointment>>();
        _mockServiceRepo = new Mock<IRepository<Service>>();
        _service = new AppointmentService(_mockAppointmentRepo.Object, _mockServiceRepo.Object);
    }

    [Fact]
    public async Task CreateAppointmentAsync_ShouldCreateAppointment_WhenServiceExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var scheduledTime = DateTime.UtcNow.AddDays(1);
        var service = new Service 
        { 
            Id = serviceId, 
            Name = "Test Service", 
            Price = 100, 
            DurationMin = 60 
        };

        _mockServiceRepo.Setup(r => r.GetByIdAsync(serviceId))
            .ReturnsAsync(service);

        _mockAppointmentRepo.Setup(r => r.AddAsync(It.IsAny<Appointment>()))
            .ReturnsAsync((Appointment a) => a);

        // Act
        var result = await _service.CreateAppointmentAsync(userId, serviceId, scheduledTime);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(serviceId, result.ServiceId);
        Assert.Equal(service.Price, result.Price);
        Assert.Equal(AppointmentStatus.Pending, result.Status);
        
        // Verify StartTime and EndTime
        var expectedStart = new DateTimeOffset(scheduledTime, TimeSpan.Zero);
        Assert.Equal(expectedStart, result.StartTime);
        Assert.Equal(expectedStart.AddMinutes(service.DurationMin), result.EndTime);

        _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Once);
    }

    [Fact]
    public async Task CreateAppointmentAsync_ShouldThrowException_WhenServiceDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var scheduledTime = DateTime.UtcNow.AddDays(1);

        _mockServiceRepo.Setup(r => r.GetByIdAsync(serviceId))
            .ReturnsAsync((Service?)null);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _service.CreateAppointmentAsync(userId, serviceId, scheduledTime));
            
        _mockAppointmentRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Never);
    }
}
