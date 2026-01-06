using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Tarot.Api.Controllers;
using Tarot.Api.Dtos;
using Tarot.Core.Entities;
using Tarot.Core.Enums;

namespace Tarot.IntegrationTests;

public class SystemVerificationTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public SystemVerificationTests(CustomWebApplicationFactory<Program> factory)
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
    public async Task Cards_Pagination_And_Search_Should_Work()
    {
        var r = await _client.GetAsync("/api/v1/cards?q=The&page=1&pageSize=5");
        r.EnsureSuccessStatusCode();
        var payload = await r.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(payload);
        Assert.True(payload!.ContainsKey("items"));
    }

    [Fact]
    public async Task ContactMessages_RateLimit_Should_Enforce()
    {
        for (int i = 0; i < 5; i++)
        {
            var dto = new { Name = "Tester", Email = $"t{i}@example.com", Message = "Hello" };
            await _client.PostAsJsonAsync("/api/v1/contactmessages", dto);
        }
        {
            var dto = new { Name = "Tester", Email = $"t6@example.com", Message = "Hello" };
            var resp = await _client.PostAsJsonAsync("/api/v1/contactmessages", dto);
            Assert.Equal(HttpStatusCode.TooManyRequests, resp.StatusCode);
        }
    }

    [Fact]
    public async Task Admin_Protected_Endpoints_Should_Succeed_With_Permissions()
    {
        WithPermissions("KNOWLEDGE_EDIT", "TRASH_MANAGE", "SCHEDULE_MANAGE", "CONSULTATION_REPLY");

        var serviceId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Tarot.Infrastructure.Data.AppDbContext>();
            db.Services.Add(new Service { Id = serviceId, Name = "SysTest Service", Price = 80m, DurationMin = 30, IsActive = true });
            db.SaveChanges();
        }

        var rReload = await _client.PostAsync("/api/v1/admin/cards/reload", content: null);
        rReload.EnsureSuccessStatusCode();

        var apptId = Guid.NewGuid();
        var start = DateTimeOffset.UtcNow.AddDays(10).AddMinutes(new Random().Next(1, 55));
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Tarot.Infrastructure.Data.AppDbContext>();
            db.Appointments.Add(new Appointment
            {
                Id = apptId,
                UserId = Guid.NewGuid(),
                ServiceId = serviceId,
                StartTime = start,
                EndTime = start.AddMinutes(30),
                Status = AppointmentStatus.Pending,
                Price = 80m,
                PaymentStatus = PaymentStatus.Unpaid
            });
            db.SaveChanges();
        }

        var blockReq = new { StartTime = DateTime.UtcNow.AddDays(1), EndTime = DateTime.UtcNow.AddDays(1).AddHours(1), Reason = "System test" };
        var rBlock = await _client.PostAsJsonAsync("/api/v1/admin/slots/block", blockReq);
        rBlock.EnsureSuccessStatusCode();

        var cardCreate = new
        {
            Name = $"Auto-{Guid.NewGuid():N}",
            ImageUrl = null as string,
            Suit = Suit.Wands,
            ArcanaType = ArcanaType.Minor,
            MeaningUpright = "U",
            MeaningReversed = "R",
            Keywords = "k",
            AdminNotes = "n"
        };
        var rCard = await _client.PostAsJsonAsync("/api/v1/cards", cardCreate);
        rCard.EnsureSuccessStatusCode();
        var card = await rCard.Content.ReadFromJsonAsync<Card>();
        Assert.NotNull(card);

        var rDel = await _client.DeleteAsync($"/api/v1/cards/{card!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, rDel.StatusCode);

        var rTrash = await _client.GetAsync("/api/v1/admin/trash?entity=cards");
        rTrash.EnsureSuccessStatusCode();
        var trashList = await rTrash.Content.ReadFromJsonAsync<List<Dictionary<string, object>>>();
        Assert.True(trashList!.Any(x => Guid.Parse(x["id"].ToString()!) == card.Id));

        var restoreReq = new { Entity = "cards", Id = card.Id };
        var rRestore = await _client.PostAsJsonAsync("/api/v1/admin/trash/restore", restoreReq);
        rRestore.EnsureSuccessStatusCode();
    }
}
