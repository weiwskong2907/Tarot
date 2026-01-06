using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tarot.Api.Dtos;
using Tarot.Core.Entities;
using Tarot.Infrastructure.Data;

namespace Tarot.IntegrationTests;

public class AppointmentsIntegrationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AppointmentsIntegrationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateAppointment_ShouldReturnOk()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            // Seed Service
            var serviceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            if (context.Services.Find(serviceId) == null)
            {
                context.Services.Add(new Service
                {
                    Id = serviceId,
                    Name = "Test Service",
                    Price = 50,
                    DurationMin = 30
                });
                context.SaveChanges();
            }
        }

        var dto = new CreateAppointmentDto
        {
            ServiceId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            StartTime = DateTime.UtcNow.AddDays(1)
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/appointments", dto);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        var appointment = await response.Content.ReadFromJsonAsync<AppointmentDto>();
        Assert.NotNull(appointment);
        Assert.Equal(dto.ServiceId, appointment.ServiceId);
        Assert.Equal(50, appointment.Price);
    }

    [Fact]
    public async Task GetMyAppointments_ShouldReturnList()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/appointments");

        // Assert
        response.EnsureSuccessStatusCode();
        var appointments = await response.Content.ReadFromJsonAsync<List<AppointmentDto>>();
        Assert.NotNull(appointments);
    }
}
