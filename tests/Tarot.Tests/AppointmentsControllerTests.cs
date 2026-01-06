using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Tarot.Api.Controllers;
using Tarot.Api.Dtos;
using Tarot.Core.Entities;
using Tarot.Core.Enums;
using Tarot.Core.Interfaces;

namespace Tarot.Tests;

public class AppointmentsControllerTests
{
    private readonly Mock<IAppointmentService> _mockAppointmentService;
    private readonly Mock<IPaymentService> _mockPaymentService;
    private readonly Mock<IRepository<Appointment>> _mockAppointmentRepo;
    private readonly Mock<IRepository<Service>> _mockServiceRepo;
    private readonly Mock<IRepository<Consultation>> _mockConsultationRepo;
    private readonly AppointmentsController _controller;
    private readonly Guid _userId;

    public AppointmentsControllerTests()
    {
        _mockAppointmentService = new Mock<IAppointmentService>();
        _mockPaymentService = new Mock<IPaymentService>();
        _mockAppointmentRepo = new Mock<IRepository<Appointment>>();
        _mockServiceRepo = new Mock<IRepository<Service>>();
        _mockConsultationRepo = new Mock<IRepository<Consultation>>();
        _controller = new AppointmentsController(_mockAppointmentService.Object, _mockPaymentService.Object, _mockAppointmentRepo.Object, _mockServiceRepo.Object, _mockConsultationRepo.Object);
        
        _userId = Guid.NewGuid();
        
        // Mock User Context
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    [Fact]
    public async Task Create_ShouldReturnOk_WhenPaymentSucceeds()
    {
        // Arrange
        var dto = new CreateAppointmentDto
        {
            ServiceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1)
        };

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            ServiceId = dto.ServiceId,
            StartTime = new DateTimeOffset(dto.StartTime),
            EndTime = new DateTimeOffset(dto.StartTime.AddHours(1)),
            Price = 100,
            Status = AppointmentStatus.Pending
        };

        _mockAppointmentService.Setup(s => s.CreateAppointmentAsync(_userId, dto.ServiceId, dto.StartTime))
            .ReturnsAsync(appointment);

        _mockPaymentService.Setup(p => p.ProcessPaymentAsync(_userId, appointment.Price, "USD"))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnDto = Assert.IsType<AppointmentDto>(okResult.Value);
        Assert.Equal(appointment.Id, returnDto.Id);
        Assert.Equal(appointment.Price, returnDto.Price);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenPaymentFails()
    {
        // Arrange
        var dto = new CreateAppointmentDto
        {
            ServiceId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1)
        };

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            ServiceId = dto.ServiceId,
            Price = 100
        };

        _mockAppointmentService.Setup(s => s.CreateAppointmentAsync(_userId, dto.ServiceId, dto.StartTime))
            .ReturnsAsync(appointment);

        _mockPaymentService.Setup(p => p.ProcessPaymentAsync(_userId, appointment.Price, "USD"))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Payment failed", badRequestResult.Value);
    }

    [Fact]
    public async Task GetMyAppointments_ShouldReturnList()
    {
        // Arrange
        var appointments = new List<Appointment>
        {
            new Appointment { Id = Guid.NewGuid(), UserId = _userId, Price = 50 },
            new Appointment { Id = Guid.NewGuid(), UserId = _userId, Price = 100 }
        };

        _mockAppointmentService.Setup(s => s.GetUserAppointmentsAsync(_userId))
            .ReturnsAsync(appointments);

        // Act
        var result = await _controller.GetMyAppointments();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var dtos = Assert.IsAssignableFrom<IEnumerable<AppointmentDto>>(okResult.Value);
        Assert.Equal(2, dtos.Count());
    }

    [Fact]
    public async Task Cancel_ShouldReturnOk_WhenCancellationSucceeds()
    {
        // Arrange
        var appointmentId = Guid.NewGuid();
        _mockAppointmentService.Setup(s => s.CancelAppointmentAsync(appointmentId, _userId, It.IsAny<string?>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Cancel(appointmentId, new CancelAppointmentDto { Reason = "User reason" });

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var val = okResult.Value; // Check properties if needed
    }
}
