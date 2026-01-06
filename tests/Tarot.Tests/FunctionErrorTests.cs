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
        var blockedRepo = new Mock<IRepository<BlockedSlot>>();
        var apptId = Guid.NewGuid();
        apptRepo.Setup(r => r.GetByIdAsync(apptId)).ReturnsAsync(new Appointment
        {
            Id = apptId,
            UserId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            Status = AppointmentStatus.Completed
        });
        var service = new AppointmentService(apptRepo.Object, svcRepo.Object, userRepo.Object, blockedRepo.Object, redis.Object, email.Object);
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
        var blockedRepo = new Mock<IRepository<BlockedSlot>>();
        var apptId = Guid.NewGuid();
        apptRepo.Setup(r => r.GetByIdAsync(apptId)).ReturnsAsync(new Appointment
        {
            Id = apptId,
            UserId = Guid.NewGuid(),
            Status = AppointmentStatus.Pending
        });
        var service = new AppointmentService(apptRepo.Object, svcRepo.Object, userRepo.Object, blockedRepo.Object, redis.Object, email.Object);
        var ok = await service.CancelAppointmentAsync(apptId, Guid.NewGuid(), null);
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
    public async Task LoyaltyService_AwardPoints_Boundary_5_To_6_ShouldBe_1_5x()
    {
        var user = new AppUser { AppointmentCount = 5, LoyaltyPoints = 0 };
        var svc = new Tarot.Infrastructure.Services.LoyaltyService();
        var pts = await svc.AwardPointsForAppointmentAsync(user, 100);
        Assert.Equal(150, pts);
        Assert.Equal(6, user.AppointmentCount);
    }

    [Fact]
    public async Task LoyaltyService_AwardPoints_Before_6_ShouldBe_1_0x()
    {
        var user = new AppUser { AppointmentCount = 4, LoyaltyPoints = 0 };
        var svc = new Tarot.Infrastructure.Services.LoyaltyService();
        var pts = await svc.AwardPointsForAppointmentAsync(user, 100);
        Assert.Equal(100, pts);
        Assert.Equal(5, user.AppointmentCount);
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

    [Fact]
    public async Task AppointmentService_Create_ShouldThrow_WhenServiceMissing()
    {
        var apptRepo = new Mock<IRepository<Appointment>>();
        var svcRepo = new Mock<IRepository<Service>>();
        var userRepo = new Mock<IRepository<AppUser>>();
        var redis = new Mock<IRedisService>();
        var email = new Mock<IEmailService>();
        var blockedRepo = new Mock<IRepository<BlockedSlot>>();
        var service = new AppointmentService(apptRepo.Object, svcRepo.Object, userRepo.Object, blockedRepo.Object, redis.Object, email.Object);
        svcRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Service?)null);
        await Assert.ThrowsAsync<Exception>(() => service.CreateAppointmentAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public async Task AppointmentService_Create_ShouldThrow_WhenLockNotAcquired()
    {
        var apptRepo = new Mock<IRepository<Appointment>>();
        var svcRepo = new Mock<IRepository<Service>>();
        var userRepo = new Mock<IRepository<AppUser>>();
        var redis = new Mock<IRedisService>();
        var email = new Mock<IEmailService>();
        var blockedRepo = new Mock<IRepository<BlockedSlot>>();
        svcRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Service { DurationMin = 30, Price = 100 });
        redis.Setup(r => r.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>())).ReturnsAsync(false);
        var service = new AppointmentService(apptRepo.Object, svcRepo.Object, userRepo.Object, blockedRepo.Object, redis.Object, email.Object);
        await Assert.ThrowsAsync<Exception>(() => service.CreateAppointmentAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public async Task AppointmentService_Create_ShouldThrow_WhenSlotOverlaps()
    {
        var apptRepo = new Mock<IRepository<Appointment>>();
        var svcRepo = new Mock<IRepository<Service>>();
        var userRepo = new Mock<IRepository<AppUser>>();
        var redis = new Mock<IRedisService>();
        var email = new Mock<IEmailService>();
        var blockedRepo = new Mock<IRepository<BlockedSlot>>();
        svcRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Service { DurationMin = 30, Price = 100 });
        redis.Setup(r => r.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>())).ReturnsAsync(true);
        apptRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Appointment, bool>>>())).ReturnsAsync(1);
        var service = new AppointmentService(apptRepo.Object, svcRepo.Object, userRepo.Object, blockedRepo.Object, redis.Object, email.Object);
        await Assert.ThrowsAsync<Exception>(() => service.CreateAppointmentAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public async Task AppointmentService_Reschedule_ShouldThrow_WhenUnauthorizedUser()
    {
        var apptRepo = new Mock<IRepository<Appointment>>();
        var svcRepo = new Mock<IRepository<Service>>();
        var userRepo = new Mock<IRepository<AppUser>>();
        var redis = new Mock<IRedisService>();
        var email = new Mock<IEmailService>();
        var blockedRepo = new Mock<IRepository<BlockedSlot>>();
        var apptId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        apptRepo.Setup(r => r.GetByIdAsync(apptId)).ReturnsAsync(new Appointment
        {
            Id = apptId,
            UserId = ownerId,
            ServiceId = Guid.NewGuid(),
            Status = AppointmentStatus.Pending
        });
        var service = new AppointmentService(apptRepo.Object, svcRepo.Object, userRepo.Object, blockedRepo.Object, redis.Object, email.Object);
        await Assert.ThrowsAsync<Exception>(() => service.RescheduleAppointmentAsync(apptId, Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public async Task AppointmentService_Reschedule_ShouldThrow_WhenAppointmentNotFound()
    {
        var apptRepo = new Mock<IRepository<Appointment>>();
        var svcRepo = new Mock<IRepository<Service>>();
        var userRepo = new Mock<IRepository<AppUser>>();
        var redis = new Mock<IRedisService>();
        var email = new Mock<IEmailService>();
        var blockedRepo = new Mock<IRepository<BlockedSlot>>();
        apptRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Appointment?)null);
        var service = new AppointmentService(apptRepo.Object, svcRepo.Object, userRepo.Object, blockedRepo.Object, redis.Object, email.Object);
        await Assert.ThrowsAsync<Exception>(() => service.RescheduleAppointmentAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public async Task AppointmentService_Reschedule_ShouldThrow_WhenServiceMissing()
    {
        var apptRepo = new Mock<IRepository<Appointment>>();
        var svcRepo = new Mock<IRepository<Service>>();
        var userRepo = new Mock<IRepository<AppUser>>();
        var redis = new Mock<IRedisService>();
        var email = new Mock<IEmailService>();
        var blockedRepo = new Mock<IRepository<BlockedSlot>>();
        var apptId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        apptRepo.Setup(r => r.GetByIdAsync(apptId)).ReturnsAsync(new Appointment
        {
            Id = apptId,
            UserId = userId,
            ServiceId = serviceId,
            Status = AppointmentStatus.Pending
        });
        svcRepo.Setup(r => r.GetByIdAsync(serviceId)).ReturnsAsync((Service?)null);
        var service = new AppointmentService(apptRepo.Object, svcRepo.Object, userRepo.Object, blockedRepo.Object, redis.Object, email.Object);
        await Assert.ThrowsAsync<Exception>(() => service.RescheduleAppointmentAsync(apptId, userId, DateTime.UtcNow));
    }

    [Fact]
    public async Task AppointmentService_Reschedule_ShouldThrow_WhenLockNotAcquired()
    {
        var apptRepo = new Mock<IRepository<Appointment>>();
        var svcRepo = new Mock<IRepository<Service>>();
        var userRepo = new Mock<IRepository<AppUser>>();
        var redis = new Mock<IRedisService>();
        var email = new Mock<IEmailService>();
        var blockedRepo = new Mock<IRepository<BlockedSlot>>();
        var apptId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        apptRepo.Setup(r => r.GetByIdAsync(apptId)).ReturnsAsync(new Appointment
        {
            Id = apptId,
            UserId = userId,
            ServiceId = serviceId,
            Status = AppointmentStatus.Pending
        });
        svcRepo.Setup(r => r.GetByIdAsync(serviceId)).ReturnsAsync(new Service { DurationMin = 30 });
        redis.Setup(r => r.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>())).ReturnsAsync(false);
        var service = new AppointmentService(apptRepo.Object, svcRepo.Object, userRepo.Object, blockedRepo.Object, redis.Object, email.Object);
        await Assert.ThrowsAsync<Exception>(() => service.RescheduleAppointmentAsync(apptId, userId, DateTime.UtcNow));
    }

    [Fact]
    public async Task AppointmentService_Reschedule_ShouldThrow_WhenSlotOverlaps()
    {
        var apptRepo = new Mock<IRepository<Appointment>>();
        var svcRepo = new Mock<IRepository<Service>>();
        var userRepo = new Mock<IRepository<AppUser>>();
        var redis = new Mock<IRedisService>();
        var email = new Mock<IEmailService>();
        var blockedRepo = new Mock<IRepository<BlockedSlot>>();
        var apptId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        apptRepo.Setup(r => r.GetByIdAsync(apptId)).ReturnsAsync(new Appointment
        {
            Id = apptId,
            UserId = userId,
            ServiceId = serviceId,
            Status = AppointmentStatus.Pending
        });
        svcRepo.Setup(r => r.GetByIdAsync(serviceId)).ReturnsAsync(new Service { DurationMin = 30 });
        redis.Setup(r => r.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>())).ReturnsAsync(true);
        apptRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Appointment, bool>>>())).ReturnsAsync(1);
        var service = new AppointmentService(apptRepo.Object, svcRepo.Object, userRepo.Object, blockedRepo.Object, redis.Object, email.Object);
        await Assert.ThrowsAsync<Exception>(() => service.RescheduleAppointmentAsync(apptId, userId, DateTime.UtcNow));
    }

    [Fact]
    public async Task AppointmentService_Reschedule_ShouldThrow_WhenLimitReached()
    {
        var apptRepo = new Mock<IRepository<Appointment>>();
        var svcRepo = new Mock<IRepository<Service>>();
        var userRepo = new Mock<IRepository<AppUser>>();
        var redis = new Mock<IRedisService>();
        var email = new Mock<IEmailService>();
        var blockedRepo = new Mock<IRepository<BlockedSlot>>();
        var apptId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        apptRepo.Setup(r => r.GetByIdAsync(apptId)).ReturnsAsync(new Appointment
        {
            Id = apptId,
            UserId = userId,
            ServiceId = serviceId,
            Status = AppointmentStatus.Pending,
            RescheduleCount = 2
        });
        svcRepo.Setup(r => r.GetByIdAsync(serviceId)).ReturnsAsync(new Service { DurationMin = 30 });
        redis.Setup(r => r.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>())).ReturnsAsync(true);
        var service = new AppointmentService(apptRepo.Object, svcRepo.Object, userRepo.Object, blockedRepo.Object, redis.Object, email.Object);
        await Assert.ThrowsAsync<Exception>(() => service.RescheduleAppointmentAsync(apptId, userId, DateTime.UtcNow));
    }

    [Fact]
    public async Task AppointmentService_Create_ShouldThrow_WhenSlotBlocked()
    {
        var apptRepo = new Mock<IRepository<Appointment>>();
        var svcRepo = new Mock<IRepository<Service>>();
        var userRepo = new Mock<IRepository<AppUser>>();
        var redis = new Mock<IRedisService>();
        var email = new Mock<IEmailService>();
        var blockedRepo = new Mock<IRepository<BlockedSlot>>();
        
        svcRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Service { DurationMin = 60, Price = 100 });
        redis.Setup(r => r.AcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>())).ReturnsAsync(true);
        
        // Setup BlockedSlot repo to return count > 0
        blockedRepo.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<BlockedSlot, bool>>>())).ReturnsAsync(1);
        
        var service = new AppointmentService(apptRepo.Object, svcRepo.Object, userRepo.Object, blockedRepo.Object, redis.Object, email.Object);
        var ex = await Assert.ThrowsAsync<Exception>(() => service.CreateAppointmentAsync(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow));
        Assert.Equal("Time slot is blocked by admin.", ex.Message);
    }
}
