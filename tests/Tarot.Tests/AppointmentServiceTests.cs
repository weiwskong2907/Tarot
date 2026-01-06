using Moq;
using Tarot.Core.Entities;
using Tarot.Core.Enums;
using Tarot.Core.Interfaces;
using Tarot.Core.Services;

namespace Tarot.Tests;

public class AppointmentServiceTests
{
    private readonly Mock<IRepository<Appointment>> _mockApptRepo;
    private readonly Mock<IRepository<Service>> _mockServiceRepo;
    private readonly Mock<IRepository<AppUser>> _mockUserRepo;
    private readonly Mock<IRepository<BlockedSlot>> _mockBlockedSlotRepo;
    private readonly Mock<IRedisService> _mockRedisService;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly AppointmentService _service;

    public AppointmentServiceTests()
    {
        _mockApptRepo = new Mock<IRepository<Appointment>>();
        _mockServiceRepo = new Mock<IRepository<Service>>();
        _mockUserRepo = new Mock<IRepository<AppUser>>();
        _mockBlockedSlotRepo = new Mock<IRepository<BlockedSlot>>();
        _mockRedisService = new Mock<IRedisService>();
        _mockEmailService = new Mock<IEmailService>();

        // Setup Redis Lock to always succeed by default
        _mockRedisService.Setup(r => r.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(true);

        // Setup default repo returns
        _mockApptRepo.Setup(r => r.ListAllAsync()).ReturnsAsync(new List<Appointment>());

        _service = new AppointmentService(
            _mockApptRepo.Object, 
            _mockServiceRepo.Object,
            _mockUserRepo.Object,
            _mockBlockedSlotRepo.Object,
            _mockRedisService.Object,
            _mockEmailService.Object
        );
    }

    [Fact]
    public async Task CreateAppointment_ShouldCreate_WhenServiceExists()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var service = new Service 
        { 
            Id = serviceId, 
            Name = "Test Service", 
            DurationMin = 30, 
            Price = 100 
        };

        _mockServiceRepo.Setup(r => r.GetByIdAsync(serviceId))
            .ReturnsAsync(service);

        // Act
        var result = await _service.CreateAppointmentAsync(userId, serviceId, DateTime.UtcNow);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(AppointmentStatus.Pending, result.Status);
        Assert.Equal(service.Price, result.Price);
        _mockApptRepo.Verify(r => r.AddAsync(It.IsAny<Appointment>()), Times.Once);
        _mockRedisService.Verify(r => r.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()), Times.Once);
        _mockRedisService.Verify(r => r.ReleaseLockAsync(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CreateAppointment_ShouldThrow_WhenServiceNotFound()
    {
        // Arrange
        var serviceId = Guid.NewGuid();
        _mockServiceRepo.Setup(r => r.GetByIdAsync(serviceId))
            .ReturnsAsync((Service?)null);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _service.CreateAppointmentAsync(Guid.NewGuid(), serviceId, DateTime.UtcNow));
    }
}
