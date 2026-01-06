using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tarot.Api.Dtos;

namespace Tarot.IntegrationTests;

public class ApiErrorVerificationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiErrorVerificationTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private void WithPermissions(params string[] permissions)
    {
        _client.DefaultRequestHeaders.Remove("X-Test-Permissions");
        _client.DefaultRequestHeaders.Add("X-Test-Permissions", string.Join(",", permissions));
    }

    [Fact]
    public async Task Auth_Register_InvalidEmail_ShouldReturnBadRequest()
    {
        var dto = new RegisterDto { Email = "invalid", FullName = "User", Password = "Passw0rd!" };
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/register", dto);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Auth_Login_InvalidPassword_ShouldReturnUnauthorized()
    {
        var email = $"user{Guid.NewGuid():N}@example.com";
        var reg = new RegisterDto { Email = email, FullName = "User", Password = "Passw0rd!" };
        await _client.PostAsJsonAsync("/api/v1/auth/register", reg);
        var login = new LoginDto { Email = email, Password = "WrongPass!" };
        var resp = await _client.PostAsJsonAsync("/api/v1/auth/login", login);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Cards_Create_WithoutPermission_ShouldReturnForbidden()
    {
        var body = new
        {
            Name = "ErrCard",
            ImageUrl = (string?)null,
            Suit = 0,
            ArcanaType = 0,
            MeaningUpright = "U",
            MeaningReversed = "R",
            Keywords = "k",
            AdminNotes = "n"
        };
        var resp = await _client.PostAsJsonAsync("/api/v1/cards", body);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task EmailTemplates_Create_DuplicateSlug_ShouldReturnBadRequest()
    {
        WithPermissions("DESIGN_EDIT");
        var slug = $"dup-{Guid.NewGuid():N}";
        var body = new { Slug = slug, SubjectTpl = "S", BodyHtml = "<b>B</b>" };
        var r1 = await _client.PostAsJsonAsync("/api/v1/emailtemplates", body);
        r1.EnsureSuccessStatusCode();
        var r2 = await _client.PostAsJsonAsync("/api/v1/emailtemplates", body);
        Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
    }

    [Fact]
    public async Task SiteSettings_GetById_NotFound_ShouldReturn404()
    {
        WithPermissions("DESIGN_EDIT");
        var resp = await _client.GetAsync($"/api/v1/sitesettings/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Admin_ReloadCards_WithoutPermission_ShouldReturnForbidden()
    {
        _client.DefaultRequestHeaders.Remove("X-Test-Permissions");
        var resp = await _client.PostAsync("/api/v1/admin/cards/reload", null);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Admin_Trash_UnknownEntity_ShouldReturnBadRequest()
    {
        WithPermissions("TRASH_MANAGE");
        var resp = await _client.GetAsync("/api/v1/admin/trash?entity=unknown");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Appointments_Reschedule_Overlapping_ShouldReturnBadRequest()
    {
        var userId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Remove("X-Test-UserId");
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId);

        var serviceId = Guid.NewGuid();
        var apptA = Guid.NewGuid();
        var apptB = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow.AddDays(5).AddHours(10);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Tarot.Infrastructure.Data.AppDbContext>();
            db.Services.Add(new Tarot.Core.Entities.Service { Id = serviceId, Name = "Svc", Price = 100m, DurationMin = 60, IsActive = true });
            db.Appointments.Add(new Tarot.Core.Entities.Appointment
            {
                Id = apptA,
                UserId = Guid.Parse(userId),
                ServiceId = serviceId,
                StartTime = start,
                EndTime = start.AddMinutes(60),
                Status = Tarot.Core.Enums.AppointmentStatus.Confirmed,
                Price = 100m
            });
            db.Appointments.Add(new Tarot.Core.Entities.Appointment
            {
                Id = apptB,
                UserId = Guid.NewGuid(),
                ServiceId = serviceId,
                StartTime = start.AddMinutes(30),
                EndTime = start.AddMinutes(90),
                Status = Tarot.Core.Enums.AppointmentStatus.Confirmed,
                Price = 100m
            });
            db.SaveChanges();
        }

        var dto = new RescheduleAppointmentDto { NewStartTime = start.AddMinutes(30).UtcDateTime };
        var resp = await _client.PostAsJsonAsync($"/api/v1/appointments/{apptA}/reschedule", dto);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Slots_InvalidService_ShouldReturnBadRequest()
    {
        var date = DateTime.UtcNow.Date;
        var resp = await _client.GetAsync($"/api/v1/slots?date={date:O}&serviceId={Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Interactive_DailyDraw_Twice_ShouldReturnBadRequest()
    {
        var userId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Remove("X-Test-UserId");
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Tarot.Infrastructure.Data.AppDbContext>();
            if (!db.Cards.Any())
            {
                db.Cards.Add(new Tarot.Core.Entities.Card { Name = "Card1", Suit = Tarot.Core.Enums.Suit.Wands, ArcanaType = Tarot.Core.Enums.ArcanaType.Minor });
                db.Cards.Add(new Tarot.Core.Entities.Card { Name = "Card2", Suit = Tarot.Core.Enums.Suit.Cups, ArcanaType = Tarot.Core.Enums.ArcanaType.Minor });
                db.SaveChanges();
            }
        }
        var r1 = await _client.PostAsync("/api/v1/daily-draw", null);
        r1.EnsureSuccessStatusCode();
        var r2 = await _client.PostAsync("/api/v1/daily-draw", null);
        Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
    }

    [Fact]
    public async Task Admin_ReplyConsultation_NotFound_ShouldReturn404()
    {
        WithPermissions("CONSULTATION_REPLY");
        var resp = await _client.PostAsJsonAsync($"/api/v1/admin/appointments/{Guid.NewGuid()}/reply", new { Message = "x" });
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Admin_Restore_Nonexistent_ShouldReturnNotFound()
    {
        WithPermissions("TRASH_MANAGE");
        var body = new { Entity = "cards", Id = Guid.NewGuid() };
        var resp = await _client.PostAsJsonAsync("/api/v1/admin/trash/restore", body);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
