using Moq;
using Tarot.Core.Entities;
using Tarot.Core.Enums;
using Tarot.Core.Interfaces;
using Tarot.Core.Services;
using Microsoft.Extensions.Configuration;

namespace Tarot.Tests;

public class FunctionErrorTests
{
    [Fact]
    public async Task AppointmentService_Reschedule_ShouldThrow_WhenCompleted()
    {
        var apptRepo = new Mock<IRepository<Appointment>>();
        var svcRepo = new Mock<IRepository<Service>>();
        var userRepo = new Mock<IRepository<AppUser>>();
        var redis = new Mock<IRedisService>();
        var email = new Mock<IEmailService>();
        var apptId = Guid.NewGuid();
        apptRepo.Setup(r => r.GetByIdAsync(apptId)).ReturnsAsync(new Appointment
        {
            Id = apptId,
            UserId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            Status = AppointmentStatus.Completed
        });
        var service = new AppointmentService(apptRepo.Object, svcRepo.Object, userRepo.Object, redis.Object, email.Object);
        await Assert.ThrowsAsync<Exception>(() => service.RescheduleAppointmentAsync(apptId, Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public async Task AppointmentService_Cancel_ShouldReturnFalse_WhenUnauthorized()
    {
        var apptRepo = new Mock<IRepository<Appointment>>();
        var svcRepo = new Mock<IRepository<Service>>();
        var userRepo = new Mock<IRepository<AppUser>>();
        var redis = new Mock<IRedisService>();
        var email = new Mock<IEmailService>();
        var apptId = Guid.NewGuid();
        apptRepo.Setup(r => r.GetByIdAsync(apptId)).ReturnsAsync(new Appointment
        {
            Id = apptId,
            UserId = Guid.NewGuid(),
            Status = AppointmentStatus.Pending
        });
        var service = new AppointmentService(apptRepo.Object, svcRepo.Object, userRepo.Object, redis.Object, email.Object);
        var ok = await service.CancelAppointmentAsync(apptId, Guid.NewGuid());
        Assert.False(ok);
    }

    [Fact]
    public async Task BlogService_Create_ShouldThrow_WhenDuplicateSlug()
    {
        var repo = new Mock<IRepository<BlogPost>>();
        repo.Setup(r => r.ListAllAsync()).ReturnsAsync(new List<BlogPost> { new BlogPost { Slug = "dup" } });
        var svc = new Tarot.Core.Services.BlogService(repo.Object);
        await Assert.ThrowsAsync<Exception>(() => svc.CreatePostAsync(new BlogPost { Slug = "dup", Title = "t" }));
    }

    [Fact]
    public async Task LoyaltyService_AwardPoints_Multiplier_ShouldIncrease()
    {
        var user = new AppUser { AppointmentCount = 10, LoyaltyPoints = 0 };
        var svc = new Tarot.Infrastructure.Services.LoyaltyService();
        var pts = await svc.AwardPointsForAppointmentAsync(user, 100);
        Assert.Equal(200, pts);
        Assert.Equal(11, user.AppointmentCount);
    }

    [Fact]
    public async Task EmailService_SendTemplateEmail_ShouldFallback_WhenTemplateMissing()
    {
        var inMemory = new Dictionary<string, string?>
        {
            { "Email:SenderName", "Test" },
            { "Email:SenderEmail", "noreply@example.com" }
            // Intentionally no Email:SmtpHost to trigger mock path
        };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();

        var templateRepo = new Mock<IRepository<EmailTemplate>>();
        templateRepo.Setup(r => r.ListAllAsync()).ReturnsAsync(new List<EmailTemplate>()); // no templates

        var svc = new Tarot.Infrastructure.Services.EmailService(config, templateRepo.Object);
        await svc.SendTemplateEmailAsync("to@example.com", "missing-slug", new { Name = "Tester" });
    }

    [Fact]
    public async Task BlogService_Delete_ShouldNotThrow_WhenIdNotFound()
    {
        var repo = new Mock<IRepository<BlogPost>>();
        var missingId = Guid.NewGuid();
        repo.Setup(r => r.GetByIdAsync(missingId)).ReturnsAsync((BlogPost?)null);

        var svc = new Tarot.Core.Services.BlogService(repo.Object);
        await svc.DeletePostAsync(missingId);

        repo.Verify(r => r.DeleteAsync(It.IsAny<BlogPost>()), Times.Never);
    }
}
