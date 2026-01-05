using Microsoft.EntityFrameworkCore;
using Tarot.Infrastructure.Data;
using Tarot.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Configure Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<AppointmentCleanupWorker>();

var host = builder.Build();
host.Run();
