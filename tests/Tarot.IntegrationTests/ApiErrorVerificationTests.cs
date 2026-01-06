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

    [Fact]
    public async Task Services_GetById_NotFound_ShouldReturn404()
    {
        var resp = await _client.GetAsync($"/api/v1/services/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Services_CRUD_ShouldSucceed()
    {
        var createBody = new { Name = "T Service", Price = 50m, DurationMin = 45, IsActive = true };
        var createResp = await _client.PostAsJsonAsync("/api/v1/services", createBody);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(created);
        var id = Guid.Parse(created!["id"].ToString()!);

        var getResp = await _client.GetAsync($"/api/v1/services/{id}");
        getResp.EnsureSuccessStatusCode();

        var updateBody = new { Name = "T Service 2", Price = 60m, DurationMin = 30, IsActive = false };
        var updateResp = await _client.PutAsJsonAsync($"/api/v1/services/{id}", updateBody);
        updateResp.EnsureSuccessStatusCode();

        var deleteResp = await _client.DeleteAsync($"/api/v1/services/{id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }

    [Fact]
    public async Task Slots_ValidService_ShouldReturnSlots()
    {
        var serviceId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Tarot.Infrastructure.Data.AppDbContext>();
            db.Services.Add(new Tarot.Core.Entities.Service { Id = serviceId, Name = "S", Price = 30m, DurationMin = 60, IsActive = true });
            db.SaveChanges();
        }
        var date = DateTime.UtcNow.Date;
        var resp = await _client.GetAsync($"/api/v1/slots?date={date:O}&serviceId={serviceId}");
        resp.EnsureSuccessStatusCode();
        var arr = await resp.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        Assert.NotNull(arr);
        Assert.True(arr!.Count > 0);
    }

    [Fact]
    public async Task Interactive_SelfReading_NotEnoughCards_ShouldReturnBadRequest()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Tarot.Infrastructure.Data.AppDbContext>();
            db.Cards.RemoveRange(db.Cards);
            db.SaveChanges();
        }
        var resp = await _client.PostAsync("/api/v1/self-reading", null);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Blog_Create_DuplicateSlug_ShouldReturnBadRequest()
    {
        WithPermissions("BLOG_MANAGE");
        var slug = $"dup-{Guid.NewGuid():N}";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Tarot.Infrastructure.Data.AppDbContext>();
            db.BlogPosts.Add(new Tarot.Core.Entities.BlogPost { Title = "t", Slug = slug, Content = "c", CreatedAt = DateTimeOffset.UtcNow });
            db.SaveChanges();
        }
        var dto = new CreateBlogPostDto { Title = "t2", Slug = slug, Content = "c2" };
        var resp = await _client.PostAsJsonAsync("/api/v1/blogposts", dto);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Appointments_Cancel_Unauthorized_ShouldReturnBadRequest()
    {
        var ownerId = Guid.NewGuid();
        var otherId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Remove("X-Test-UserId");
        _client.DefaultRequestHeaders.Add("X-Test-UserId", otherId);
        var serviceId = Guid.NewGuid();
        var apptId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Tarot.Infrastructure.Data.AppDbContext>();
            db.Services.Add(new Tarot.Core.Entities.Service { Id = serviceId, Name = "S", Price = 10m, DurationMin = 30, IsActive = true });
            db.Appointments.Add(new Tarot.Core.Entities.Appointment
            {
                Id = apptId,
                UserId = ownerId,
                ServiceId = serviceId,
                StartTime = DateTimeOffset.UtcNow.AddDays(1),
                EndTime = DateTimeOffset.UtcNow.AddDays(1).AddMinutes(30),
                Status = Tarot.Core.Enums.AppointmentStatus.Pending,
                Price = 10m
            });
            db.SaveChanges();
        }
        var resp = await _client.PostAsJsonAsync($"/api/v1/appointments/{apptId}/cancel", new CancelAppointmentDto { Reason = "Test" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Appointments_Reschedule_Limit_ShouldReturnBadRequest()
    {
        var userId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Remove("X-Test-UserId");
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        var serviceId = Guid.NewGuid();
        var apptId = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow.AddDays(3).AddHours(11);
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Tarot.Infrastructure.Data.AppDbContext>();
            db.Services.Add(new Tarot.Core.Entities.Service { Id = serviceId, Name = "Svc", Price = 50m, DurationMin = 30, IsActive = true });
            db.Appointments.Add(new Tarot.Core.Entities.Appointment
            {
                Id = apptId,
                UserId = Guid.Parse(userId),
                ServiceId = serviceId,
                StartTime = start,
                EndTime = start.AddMinutes(30),
                Status = Tarot.Core.Enums.AppointmentStatus.Pending,
                Price = 50m,
                RescheduleCount = 2
            });
            db.SaveChanges();
        }
        var dto = new RescheduleAppointmentDto { NewStartTime = DateTime.UtcNow.AddDays(4) };
        var resp = await _client.PostAsJsonAsync($"/api/v1/appointments/{apptId}/reschedule", dto);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Appointments_Create_PaymentFail_ShouldReturnBadRequest()
    {
        Environment.SetEnvironmentVariable("ENABLE_PAYMENT", "true");
        Environment.SetEnvironmentVariable("MOCK_PAYMENT_FAIL", "true");
        var userId = Guid.NewGuid().ToString();
        _client.DefaultRequestHeaders.Remove("X-Test-UserId");
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
        var serviceId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Tarot.Infrastructure.Data.AppDbContext>();
            db.Services.Add(new Tarot.Core.Entities.Service { Id = serviceId, Name = "PaySvc", Price = 80m, DurationMin = 30, IsActive = true });
            db.SaveChanges();
        }
        var dto = new CreateAppointmentDto { ServiceId = serviceId, StartTime = DateTime.UtcNow.AddDays(2) };
        var resp = await _client.PostAsJsonAsync("/api/v1/appointments", dto);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Environment.SetEnvironmentVariable("MOCK_PAYMENT_FAIL", null);
        Environment.SetEnvironmentVariable("ENABLE_PAYMENT", null);
    }
}
